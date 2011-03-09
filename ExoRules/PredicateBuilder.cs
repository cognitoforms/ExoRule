using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Linq;
using ExoGraph;

namespace ExoRule
{
	#region PredicateBuilder

	internal class PredicateBuilder
	{
		static readonly string[] CollectionChanges = new string[] { "Add", "Clear", "Insert", "Remove", "Merge" };
		List<string> getPaths = new List<string>();
		List<string> setPaths = new List<string>();
		Predicate<MethodBase> methodFilter;
		bool isSetRule;

		private PredicateBuilder() { }

		public static List<string> GetPredicates(MethodBase method, Predicate<MethodBase> methodFilter)
		{
			MethodIntrospector introspector = new MethodIntrospector(method, methodFilter);
			return (new PredicateBuilder()).GetPredicates(method, introspector.Vertices[method.IsStatic ? 0 : 1], methodFilter);
		}

		List<string> GetPredicates(MethodBase method, Vertex root, Predicate<MethodBase> methodFilter)
		{
			List<string> returnValue = new List<string>();
			this.methodFilter = methodFilter;
			isSetRule = method.DeclaringType.Name.StartsWith("Set");

			BuildPaths(root, "");
			foreach (string predicate in getPaths)
			{
				string value = predicate;
				if (value.Length > root.Name.Length + 2)
				{
					value = value.Substring(root.Name.Length + 2);
					returnValue.Add(value);
				}
			}

				foreach (string predicate in setPaths)
					if (predicate.Length > root.Name.Length + 2)
						returnValue.Add(predicate.Substring(root.Name.Length + 2) + " return");

			return returnValue;
		}

		void BuildPaths(Vertex vertex, string path)
		{
			path += '.' + vertex.Name;
			Vertex currentVertex = vertex;
			if (typeof(ICollection).IsAssignableFrom(vertex.Type))
			{
				currentVertex = ConsolidateItemVertex(vertex, path);
				if (currentVertex == null)
					return;
			}

			bool pathAdded = false;
			foreach (Edge edge in currentVertex.Edges)
			{
				if (methodFilter(edge.Method))
				{
					if (edge.Child.Edges.Count == 0)
					{
						pathAdded = true;
						if (edge.Method.Name.StartsWith("get_"))
							getPaths.Add(path + '.' + edge.Child.Name);
						else if (edge.Method.Name.StartsWith("set_"))
							setPaths.Add(path + '.' + edge.Child.Name);
					}
					else if (edge.Method.Name.StartsWith("get_"))
					{
						pathAdded = true;
						BuildPaths(edge.Child, path);
					}
				}
			}

			if (!pathAdded)
				getPaths.Add(path);
		}

		Vertex ConsolidateItemVertex(Vertex collectionVertex, string path)
		{
			bool pathAdded = false;
			if (isSetRule)
			{
				foreach (Edge e in collectionVertex.Edges)
				{
					if (IsCollectionUpdate(e) && !pathAdded)
					{
						pathAdded = true;
						setPaths.Add(path);
					}
				}
			}

			if (typeof(IList).IsAssignableFrom(collectionVertex.Type))
			{
				Type itemType = collectionVertex.Type.GetProperty("Item", new Type[] { typeof(int) }).PropertyType;
				Vertex returnValue = new Vertex();
				foreach (Vertex item in GetListItems(collectionVertex, itemType))
					foreach (Edge edgeToCombine in item.Edges)
						if (edgeToCombine.Method.Name.StartsWith("get_"))
							if (!returnValue.Edges.Contains(edgeToCombine))
								returnValue.Edges.Add(edgeToCombine);

				if (returnValue.Edges.Count > 0)
					return returnValue;
			}

			if (!pathAdded || !isSetRule)
				getPaths.Add(typeof(IList).IsAssignableFrom(collectionVertex.Type) ? path : path);

			return null;
		}

		bool IsCollectionUpdate(Edge edge)
		{
			if (edge.Method == null)
				return false;

			if (!typeof(ICollection).IsAssignableFrom(edge.Method.DeclaringType))
				return false;

			string methodName = edge.Method.Name;
			if (methodName.StartsWith("Add") || methodName.StartsWith("Clear") || methodName.StartsWith("Insert")
				|| methodName.StartsWith("Remove") || methodName.StartsWith("Merge"))
				return true;

			return false;
		}

		IEnumerable<Vertex> GetListItems(Vertex listVertex, Type itemType)
		{
			foreach (Edge edge in listVertex.Edges)
			{
				if (edge.Child.Type == itemType)
					yield return edge.Child;
				else
					foreach (Vertex itemVertex in GetListItems(edge.Child, itemType))
						yield return itemVertex;
			}
		}

		#region MethodIntrospector

		class MethodIntrospector
		{
			#region Fields

			static readonly Regex indexRegex = new Regex(@"(?<op>ldarg|stloc|ldloc)[a]?(\.(?<index>[0-3s]))?", RegexOptions.Compiled | RegexOptions.Singleline);
			MethodBase enclosingMethod;
			List<Vertex> vertices;
			Vertex returnVertex;
			Predicate<MethodBase> methodFilter;
			MethodBody methodBody;
			SortedList<int, ILInstruction> instructions = new SortedList<int, ILInstruction>();
			Stack<Vertex> evaluationStack = new Stack<Vertex>();
			Dictionary<int, Vertex> localVariables = new Dictionary<int, Vertex>();
			SortedList<int, Stack<Vertex>> branchTargetsToVisit = new SortedList<int, Stack<Vertex>>();
			Stack<MethodBase> nestedMethodCalls;
			List<int> offsetsInReturnPath;

			#endregion

			#region Constructors

			public MethodIntrospector(MethodBase method)
				: this(method, delegate { return false; })
			{ }

			public MethodIntrospector(MethodBase method, Predicate<MethodBase> methodFilter)
			{
				this.enclosingMethod = method;
				this.methodFilter = methodFilter;
				this.nestedMethodCalls = new Stack<MethodBase>();
				this.methodBody = method.GetMethodBody();

				vertices = new List<Vertex>();
				if (!method.IsStatic)
				{
					Vertex _this = new Vertex();
					_this.Type = method.DeclaringType;
					_this.Name = "this";
					vertices.Add(_this);
				}
				foreach (ParameterInfo parameter in method.GetParameters())
				{
					Vertex vertex = new Vertex();
					vertex.Type = parameter.ParameterType;
					vertex.Name = parameter.Name;
					vertices.Add(vertex);
				}

				Analysis();
			}

			MethodIntrospector(MethodBase method, List<Vertex> vertices, Predicate<MethodBase> methodFilter, Stack<MethodBase> methodCallStack)
			{
				//System.Diagnostics.Debug.WriteLine("************ " + method.Name + " ************");
				this.enclosingMethod = method;
				this.vertices = vertices;
				this.methodFilter = methodFilter;
				this.nestedMethodCalls = methodCallStack;
				this.methodBody = method.GetMethodBody();

				Analysis();
				//System.Diagnostics.Debug.WriteLine("*************************************");
			}
			#endregion

			#region Methods

			public Vertex ReturnVertex
			{
				get { return returnVertex; }
			}

			public List<Vertex> Vertices
			{
				get { return vertices; }
			}

			void Analysis()
			{
				List<int> branchTargetOffsets = new List<int>();
				List<int> returnInstructionOffsets = new List<int>();

				foreach (ILInstruction instruction in new ILReader(enclosingMethod))
				{
					instructions.Add(instruction.Offset, instruction);

					int targetOffset = -1;
					switch (instruction.OpCode.FlowControl)
					{
						case FlowControl.Return:
							returnInstructionOffsets.Add(instruction.Offset);
							break;

						case FlowControl.Branch:
							targetOffset = (instruction as BranchInstruction).Target;
							if (!branchTargetOffsets.Contains(targetOffset))
								branchTargetOffsets.Add(targetOffset);
							break;

						case FlowControl.Cond_Branch:
							if (!branchTargetOffsets.Contains(instruction.Next))
								branchTargetOffsets.Add(instruction.Next);

							if (instruction is BranchInstruction)
							{
								targetOffset = (instruction as BranchInstruction).Target;
								if (!branchTargetOffsets.Contains(targetOffset))
									branchTargetOffsets.Add(targetOffset);
							}
							break;

						default:
							break;
					}
				}

				offsetsInReturnPath = new List<int>();
				branchTargetOffsets.Sort();
				foreach (int returnOffset in returnInstructionOffsets)
				{
					int closest = returnOffset;
					foreach (int branchTarget in branchTargetOffsets)
						if (branchTarget < returnOffset)
							closest = branchTarget;

					int start = instructions.Keys.IndexOf(closest);
					int end = instructions.Keys.IndexOf(returnOffset);
					for (int i = start; i <= end; i++)
					{
						if (!offsetsInReturnPath.Contains(instructions.Keys[i]))
							offsetsInReturnPath.Add(instructions.Keys[i]);
					}
				}

				if (instructions.Count > 0)
				{
					nestedMethodCalls.Push(enclosingMethod);
					Analysis(instructions[0], delegate(ILInstruction next) { return next != null; });
					nestedMethodCalls.Pop();
				}
			}

			void Analysis(ILInstruction instruction, Predicate<ILInstruction> canMoveNext)
			{
				while (canMoveNext(instruction))
				{
					//System.Diagnostics.Debug.WriteLine(PrintInstruction(instruction));
					instruction.Visited = true;
					ManageEvaluationStack(instruction);
					instruction = GetNext(instruction);
				}
			}

			/// <summary>
			/// Executes the specified instruction and returns the next instruction to execute.
			/// </summary>
			/// <param name="instruction">The instruction to execute.</param>
			/// <returns>Returns the next instruction to execute, null when complete.</returns>
			/// <remarks>Instructions in catch or filter blocks are ignored.</remarks>
			ILInstruction GetNext(ILInstruction instruction)
			{
				// find the next instruction to execute
				int targetOffest = -1;
				switch (instruction.OpCode.FlowControl)
				{
					case FlowControl.Branch:
						// ensures that the finally blocks are executed when control leaves a try block.
						if (instruction.OpCode.Name.StartsWith("leave"))
							ExecuteFinallyBlocks(instruction as BranchInstruction);

						// unconditionally transfers control to a target instruction
						targetOffest = (instruction as BranchInstruction).Target;
						break;

					case FlowControl.Cond_Branch:
						targetOffest = GetFirstConditionalBranchTarget(instruction);
						break;

					case FlowControl.Return:
						break;

					default:
						targetOffest = instruction.Next;
						break;
				}

				ILInstruction next = targetOffest >= 0 && targetOffest <= instructions.Keys.Max() ? instructions[targetOffest] : null;

				// run instructions that have not been executed
				if (next != null && (!next.Visited || offsetsInReturnPath.Contains(targetOffest)))
					return next;

				// run the conditional branch targets
				if (branchTargetsToVisit.Count > 0)
				{
					KeyValuePair<int, Stack<Vertex>> branch = branchTargetsToVisit.ElementAt(0);
					branch.Value.Reverse();
					evaluationStack = branch.Value;
					branchTargetsToVisit.RemoveAt(0);
					return instructions[branch.Key];
				}

				// all reachable instructions have been visited. 
				return null;
			}

			int GetFirstConditionalBranchTarget(ILInstruction instruction)
			{
				int returnValue;
				// register the branch target for re-run after normal control flow completes. 
				if (instruction is SwitchInstruction)
				{
					SwitchInstruction switchIL = instruction as SwitchInstruction;
					// run the first "case" target and register other targets(including "default") for re-run
					if (switchIL.Cases.Length > 1)
					{
						returnValue = switchIL.Next + switchIL.Cases[0];
						branchTargetsToVisit[switchIL.Next] = new Stack<Vertex>(evaluationStack);

						for (int i = 1; i < switchIL.Cases.Length; i++)
							branchTargetsToVisit[switchIL.Next + switchIL.Cases[i]] = new Stack<Vertex>(evaluationStack);
					}
					else
						returnValue = switchIL.Next;
				}
				else
				{
					BranchInstruction branchIL = instruction as BranchInstruction;
					if (branchIL.Offset > branchIL.Target)
					{
						returnValue = branchIL.Target;
						branchTargetsToVisit[branchIL.Next] = new Stack<Vertex>(evaluationStack);
					}
					else
					{
						returnValue = branchIL.Next;
						branchTargetsToVisit[branchIL.Target] = new Stack<Vertex>(evaluationStack);
					}
				}
				return returnValue;
			}

			/// <summary>
			/// Executes instructions in finally blocks that protect the region the specified leave instruction exits.
			/// </summary>
			/// <param name="leaveIL">The IL instruction that exits a protected region of code. </param>
			void ExecuteFinallyBlocks(BranchInstruction leaveIL)
			{
				SortedList<int, ExceptionHandlingClause> finallyblocks = new SortedList<int, ExceptionHandlingClause>();

				// finds nested finally blocks, candidates should meet two conditions:
				// 1. the leave instruction is in the protected region of the finally block
				// 2. the branch target is out of the the protected region of the finally block
				foreach (ExceptionHandlingClause clause in methodBody.ExceptionHandlingClauses)
				{
					if (clause.Flags == ExceptionHandlingClauseOptions.Finally)
					{
						if (leaveIL.Offset >= clause.TryOffset && leaveIL.Offset <= clause.TryOffset + clause.TryLength
							&& leaveIL.Target >= clause.HandlerOffset + clause.HandlerLength)
						{
							ILInstruction instruction = instructions[clause.HandlerOffset];
							if (!instruction.Visited)
							{
								finallyblocks.Add(clause.TryOffset, clause);
							}
						}
					}
				}

				// executes the most deeply nested blocks before the blocks that enclose them
				foreach (ExceptionHandlingClause clause in finallyblocks.Values.Reverse())
				{
					ILInstruction instruction = instructions[clause.HandlerOffset];
					if (!instruction.Visited)
					{
						//System.Diagnostics.Debug.WriteLine("*** enter finally *** " + PrintInstruction(instruction));
						Analysis(instruction, delegate(ILInstruction next) { return next.OpCode.Name != "endfinally"; });
						//System.Diagnostics.Debug.WriteLine("*** end finally *** ");
					}
				}
			}

			void ManageEvaluationStack(ILInstruction instruction)
			{
				if (indexRegex.IsMatch(instruction.OpCode.Name))
				{
					StoreOrLoad(instruction);
				}
				else if (instruction.OpCode.Name == "ret")
				{
					if (enclosingMethod is MethodInfo && ((MethodInfo)enclosingMethod).ReturnType != typeof(void))
					{
						Vertex ret = evaluationStack.Pop();
						if (returnVertex == null)
							returnVertex = ret;
						else if (returnVertex == Vertex.Empty && ret != Vertex.Empty)
							returnVertex = ret;
					}
				}
				else if (instruction is MethodInstruction)
				{
					MethodBase method = (instruction as MethodInstruction).Method;
					// pops the evaluation stack, builds a list of vertices that represents argument list to the callee
					List<Vertex> parameters = new List<Vertex>();
					for (int i = 0; i < method.GetParameters().Length; i++)
						parameters.Insert(0, evaluationStack.Pop());

					// pops "this" pointer or the object reference
					// and adds it to the argument list as the first argument
					if (!method.IsConstructor && !method.IsStatic)
						parameters.Insert(0, evaluationStack.Pop());

					Vertex returnValue;
					if (method.IsConstructor)
						returnValue = CallConstructor((ConstructorInfo)method, parameters);
					else
						returnValue = CallMethod((MethodInfo)method, parameters);

					if (returnValue != null)
						evaluationStack.Push(returnValue);
				}
				else if (instruction.OpCode.Name == "castclass" || instruction.OpCode.Name == "isinst")
				{
				}
				else
				{
					for (int i = 0; i < GetCountAffected(instruction.OpCode.StackBehaviourPop); i++)
						evaluationStack.Pop();

					for (int i = 0; i < GetCountAffected(instruction.OpCode.StackBehaviourPush); i++)
						evaluationStack.Push(Vertex.Empty);
				}
			}

			Vertex CallConstructor(ConstructorInfo ctor, List<Vertex> parameters)
			{
				if (methodFilter(ctor) && !nestedMethodCalls.Contains(ctor))
				{
					// appends parameters with the vertices introduced by instructions in the constructor
					new MethodIntrospector(ctor, parameters, methodFilter, nestedMethodCalls);
				}

				Vertex vertex = new Vertex();
				vertex.Type = ctor.DeclaringType;
				vertex.Name = ctor.DeclaringType.Name;
				return vertex;
			}

			Vertex CallMethod(MethodInfo method, List<Vertex> parameters)
			{
				Vertex result = null;
				if (CanInstropectMethod(method, parameters) && !nestedMethodCalls.Contains(method))
					result = (new MethodIntrospector(method, parameters, methodFilter, nestedMethodCalls)).ReturnVertex;
				else if (!method.IsStatic && parameters[0] != Vertex.Empty)
					result = FindChild(method, parameters);

				return method.ReturnType != typeof(void) ? result ?? Vertex.Empty : null;
			}

			Vertex FindChild(MethodInfo method, List<Vertex> parameters)
			{
				Vertex parent = parameters[0];
				foreach (Edge e in parent.Edges)
					if (e.Name == method.Name)
						return e.Child;

				Vertex child = new Vertex();
				child.Type = method.ReturnParameter.ParameterType;
				if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
					child.Name = method.Name.Substring(4);
				else
					child.Name = method.Name;

				Edge edge = new Edge();
				edge.Parent = parent;
				edge.Child = child;
				edge.Name = method.Name;
				edge.Method = method;
				parent.Edges.Add(edge);
				child.InEdges.Add(edge);
				return child;
			}

			bool CanInstropectMethod(MethodInfo method, List<Vertex> parameters)
			{
				if (!methodFilter(method))
					return false;

				if (!method.Name.StartsWith("get_") && !method.Name.StartsWith("set_"))
					return true;

				if (method.Name == "get_Item" && parameters.Count > 0 && typeof(IList).IsAssignableFrom(parameters[0].Type))
					return true;

				return false;
			}

			int GetCountAffected(StackBehaviour behaviour)
			{
				switch (behaviour)
				{
					case StackBehaviour.Pop0:
					case StackBehaviour.Push0:
						return 0;

					case StackBehaviour.Pop1:
					case StackBehaviour.Popi:
					case StackBehaviour.Popref:
					case StackBehaviour.Varpop:
						return 1;

					case StackBehaviour.Pop1_pop1:
					case StackBehaviour.Popi_pop1:
					case StackBehaviour.Popi_popi:
					case StackBehaviour.Popi_popi8:
					case StackBehaviour.Popi_popr4:
					case StackBehaviour.Popi_popr8:
					case StackBehaviour.Popref_pop1:
					case StackBehaviour.Popref_popi:
						return 2;

					case StackBehaviour.Popi_popi_popi:
					case StackBehaviour.Popref_popi_pop1:
					case StackBehaviour.Popref_popi_popi:
					case StackBehaviour.Popref_popi_popi8:
					case StackBehaviour.Popref_popi_popr4:
					case StackBehaviour.Popref_popi_popr8:
					case StackBehaviour.Popref_popi_popref:
						return 3;

					case StackBehaviour.Push1:
					case StackBehaviour.Pushi:
					case StackBehaviour.Pushi8:
					case StackBehaviour.Pushr4:
					case StackBehaviour.Pushr8:
					case StackBehaviour.Pushref:
					case StackBehaviour.Varpush:
						return 1;

					case StackBehaviour.Push1_push1:
						return 2;

					default:
						return 0;
				}
			}

			void StoreOrLoad(ILInstruction instruction)
			{
				Match match = indexRegex.Match(instruction.OpCode.Name);
				if (!match.Success)
					return;

				int index = instruction is InlineVarInstruction
					? (instruction as InlineVarInstruction).Index
					: int.Parse(match.Groups["index"].Value);

				switch (match.Groups["op"].Value)
				{
					case "ldarg":
						evaluationStack.Push(vertices[index]);
						break;

					case "stloc":
						localVariables[index] = evaluationStack.Pop();
						break;

					case "ldloc":
						if (!localVariables.ContainsKey(index))
						{
							Vertex vertex = new Vertex();
							vertex.Type = methodBody.LocalVariables[index].LocalType;
							localVariables[index] = vertex;
						}
						evaluationStack.Push(localVariables[index]);
						break;
				}
			}

			string PrintInstruction(ILInstruction instruction)
			{
				return string.Format("{0} - {1}|{2} - {3}",
					instruction,
					instruction.OpCode.StackBehaviourPop,
					instruction.OpCode.StackBehaviourPush,
					PrintEvaluationStack());
			}

			string PrintEvaluationStack()
			{
				StringBuilder sb = new StringBuilder();
				foreach (Vertex t in evaluationStack)
					sb.AppendFormat(",{0}", t.Type != null ? t.Type.Name : "Empty");
				return sb.ToString();
			}

			#endregion
		}

		#endregion

		#region Edge

		/// <summary>
		/// Represents the relationship between to vertices (a property).
		/// </summary>
		public class Edge
		{
			public string Name;
			public Vertex Parent;
			public Vertex Child;
			public MethodInfo Method;

			public override string ToString()
			{
				return string.Format("Name = {0}", Name);
			}

			public override bool Equals(object obj)
			{
				Edge that = obj as Edge;
				return that != null && Method == that.Method;
			}

			public override int GetHashCode()
			{
				return Method.GetHashCode();
			}
		}

		#endregion

		#region Vertex

		/// <summary>
		/// Represents a specific type, which has child edges (properties).
		/// </summary>
		public class Vertex
		{
			public static Vertex Empty = new Vertex();
			public Type Type;
			public List<Edge> Edges = new List<Edge>();
			public string Name;
			internal List<Edge> InEdges = new List<Edge>();

			public override string ToString()
			{
				return string.Format("Name = {0}; Edges = {1}", Name, Edges.Count);
			}

			public void PrintGraph(bool parent)
			{
				if (parent)
					PrintParents(0, 0);
				else
					PrintChildren(0, 0);
			}

			void PrintChildren(int spaces, int dashes)
			{
				foreach (Edge e in Edges)
				{
					string message = string.Format("{2}{3}{0} - {1}", e.Child.Name, e.Name, "".PadLeft(spaces), "".PadLeft(dashes, '-'));
					System.Diagnostics.Debug.WriteLine(message);
					e.Child.PrintChildren(spaces + dashes + e.Child.Name.Length / 2, e.Child.Name.Length / 2);
				}
			}

			void PrintParents(int spaces, int dashes)
			{
				foreach (Edge e in InEdges)
				{
					string message = string.Format("{2}{3}{0} - {1}", e.Parent.Name, e.Name, "".PadLeft(spaces), "".PadLeft(dashes, '-'));
					System.Diagnostics.Debug.WriteLine(message);
					e.Parent.PrintParents(spaces + dashes + e.Child.Name.Length / 2, e.Parent.Name.Length / 2);
				}
			}
		}

		#endregion

		#region ILReader

		public class ILReader : IEnumerable<ILInstruction>
		{
			static OpCode[] s_OneByteOpCodes = new OpCode[0x100];
			static OpCode[] s_TwoByteOpCodes = new OpCode[0x100];

			Byte[] m_byteArray;
			Int32 m_position;
			MethodBase m_enclosingMethod;

			static ILReader()
			{
				foreach (FieldInfo fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
				{
					OpCode opCode = (OpCode)fi.GetValue(null);
					UInt16 value = (UInt16)opCode.Value;
					if (value < 0x100)
						s_OneByteOpCodes[value] = opCode;
					else if ((value & 0xff00) == 0xfe00)
						s_TwoByteOpCodes[value & 0xff] = opCode;
				}
			}

			public ILReader(MethodBase enclosingMethod)
			{
				this.m_enclosingMethod = enclosingMethod;
				MethodBody methodBody = m_enclosingMethod.GetMethodBody();
				this.m_byteArray = (methodBody == null) ? new Byte[0] : methodBody.GetILAsByteArray();
				this.m_position = 0;
			}

			public IEnumerator<ILInstruction> GetEnumerator()
			{
				while (m_position < m_byteArray.Length)
					yield return Next();

				m_position = 0;
				yield break;
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			ILInstruction Next()
			{
				Int32 offset = m_position;
				OpCode opCode = OpCodes.Nop;
				Int32 token = 0;

				// read first 1 or 2 bytes as opCode
				Byte code = ReadByte();
				if (code != 0xFE)
					opCode = s_OneByteOpCodes[code];
				else
				{
					code = ReadByte();
					opCode = s_TwoByteOpCodes[code];
				}

				switch (opCode.OperandType)
				{
					case OperandType.InlineNone: return new ILInstruction(m_enclosingMethod, offset, opCode, m_position - offset);
					case OperandType.ShortInlineI: Byte int8 = ReadByte(); return new ILInstruction(m_enclosingMethod, offset, opCode, m_position - offset);
					case OperandType.InlineI: Int32 int32 = ReadInt32(); return new ILInstruction(m_enclosingMethod, offset, opCode, m_position - offset);
					case OperandType.InlineI8: Int64 int64 = ReadInt64(); return new ILInstruction(m_enclosingMethod, offset, opCode, m_position - offset);
					case OperandType.ShortInlineR: Single float32 = ReadSingle(); return new ILInstruction(m_enclosingMethod, offset, opCode, m_position - offset);
					case OperandType.InlineR: Double float64 = ReadDouble(); return new ILInstruction(m_enclosingMethod, offset, opCode, m_position - offset);
					case OperandType.InlineString: token = ReadInt32(); return new InlineStringInstruction(m_enclosingMethod, offset, opCode, m_position - offset, token);
					case OperandType.InlineSig: token = ReadInt32(); return new ILInstruction(m_enclosingMethod, offset, opCode, m_position - offset);
					case OperandType.InlineField: token = ReadInt32(); return new ILInstruction(m_enclosingMethod, offset, opCode, m_position - offset);
					//case OperandType.InlineField: token = ReadInt32(); return new InlineFieldInstruction(m_enclosingMethod, offset, opCode, m_position - offset, token);
					case OperandType.InlineTok: token = ReadInt32(); return new ILInstruction(m_enclosingMethod, offset, opCode, m_position - offset);

					case OperandType.ShortInlineBrTarget: SByte shortDelta = ReadSByte(); return new BranchInstruction(m_enclosingMethod, offset, opCode, m_position - offset, shortDelta);
					case OperandType.InlineBrTarget: Int32 delta = ReadInt32(); return new BranchInstruction(m_enclosingMethod, offset, opCode, m_position - offset, delta);
					case OperandType.ShortInlineVar: Byte index8 = ReadByte(); return new InlineVarInstruction(m_enclosingMethod, offset, opCode, m_position - offset, index8);
					case OperandType.InlineVar: UInt16 index16 = ReadUInt16(); return new InlineVarInstruction(m_enclosingMethod, offset, opCode, m_position - offset, index16);

					case OperandType.InlineType: token = ReadInt32(); return new TypeInstruction(m_enclosingMethod, offset, opCode, token, m_position - offset);

					case OperandType.InlineMethod: token = ReadInt32(); return new MethodInstruction(m_enclosingMethod, offset, opCode, token, m_position - offset);

					case OperandType.InlineSwitch:
						Int32 cases = ReadInt32();
						Int32[] deltas = new Int32[cases];
						for (Int32 i = 0; i < cases; i++) deltas[i] = ReadInt32();
						return new SwitchInstruction(m_enclosingMethod, offset, opCode, m_position - offset, deltas);

					default:
						throw new BadImageFormatException("unexpected OperandType " + opCode.OperandType);
				}
			}

			Byte ReadByte() { return (Byte)m_byteArray[m_position++]; }
			SByte ReadSByte() { return (SByte)ReadByte(); }

			UInt16 ReadUInt16() { m_position += 2; return BitConverter.ToUInt16(m_byteArray, m_position - 2); }
			UInt32 ReadUInt32() { m_position += 4; return BitConverter.ToUInt32(m_byteArray, m_position - 4); }
			UInt64 ReadUInt64() { m_position += 8; return BitConverter.ToUInt64(m_byteArray, m_position - 8); }

			Int32 ReadInt32() { m_position += 4; return BitConverter.ToInt32(m_byteArray, m_position - 4); }
			Int64 ReadInt64() { m_position += 8; return BitConverter.ToInt64(m_byteArray, m_position - 8); }

			Single ReadSingle() { m_position += 4; return BitConverter.ToSingle(m_byteArray, m_position - 4); }
			Double ReadDouble() { m_position += 8; return BitConverter.ToDouble(m_byteArray, m_position - 8); }
		}

		#endregion

		#region ILInstruction

		public class ILInstruction
		{
			MethodBase enclosingMethod;
			int offset;
			OpCode opCode;
			int size;
			public bool Visited;

			public ILInstruction(MethodBase enclosingMethod, int offset, OpCode opCode, int size)
			{
				this.enclosingMethod = enclosingMethod;
				this.offset = offset;
				this.opCode = opCode;
				this.size = size;
			}

			public MethodBase EnclosingMethod
			{
				get
				{
					return enclosingMethod;
				}
			}

			public OpCode OpCode
			{
				get
				{
					return opCode;
				}
			}

			public int Offset
			{
				get { return offset; }
			}

			public int Next
			{
				get { return offset + size; }
			}

			public override string ToString()
			{
				return string.Format("{0}<{3},{4}> {1}({2})", offset.ToString("x"), opCode.Name, opCode.OperandType, offset, size);
			}
		}

		#endregion

		#region MethodInstruction

		public class MethodInstruction : ILInstruction
		{
			MethodBase method;

			public MethodInstruction(MethodBase enclosingMethod, int offset, OpCode opCode, int token, int size)
				: base(enclosingMethod, offset, opCode, size)
			{
				Type[] genericTypeArguments = enclosingMethod.DeclaringType.IsGenericType ? enclosingMethod.DeclaringType.GetGenericArguments() : null;
				Type[] genericMethodArguments = enclosingMethod.IsGenericMethod ? enclosingMethod.GetGenericArguments() : null;
				this.method = enclosingMethod.DeclaringType.Assembly.ManifestModule.ResolveMethod(token, genericTypeArguments, genericMethodArguments);
			}

			public MethodBase Method
			{
				get
				{
					return method;
				}
			}

			public override string ToString()
			{
				return base.ToString() + ":  " + Method.DeclaringType.FullName + "." + Method.Name;
			}
		}

		#endregion

		#region TypeInstruction

		public class TypeInstruction : ILInstruction
		{
			Type type;

			public TypeInstruction(MethodBase enclosingMethod, int offset, OpCode opCode, int token, int size)
				: base(enclosingMethod, offset, opCode, size)
			{
				Type[] genericTypeArguments = enclosingMethod.DeclaringType.IsGenericType ? enclosingMethod.DeclaringType.GetGenericArguments() : null;
				Type[] genericMethodArguments = enclosingMethod.IsGenericMethod ? enclosingMethod.GetGenericArguments() : null;
				this.type = enclosingMethod.DeclaringType.Assembly.ManifestModule.ResolveType(token, genericTypeArguments, genericMethodArguments);
			}

			public Type Type
			{
				get
				{
					return type;
				}
			}

			public override string ToString()
			{
				return base.ToString() + ":  " + Type.Name;
			}
		}

		#endregion

		#region InlineFieldInstruction

		public class InlineFieldInstruction : ILInstruction
		{
			FieldInfo field;

			public InlineFieldInstruction(MethodBase enclosingMethod, int offset, OpCode opCode, int size, int token)
				: base(enclosingMethod, offset, opCode, size)
			{
				Type[] genericTypeArguments = enclosingMethod.DeclaringType.IsGenericType ? enclosingMethod.DeclaringType.GetGenericArguments() : null;
				Type[] genericMethodArguments = enclosingMethod.IsGenericMethod ? enclosingMethod.GetGenericArguments() : null;
				field = enclosingMethod.DeclaringType.Assembly.ManifestModule.ResolveField(token, genericTypeArguments, genericMethodArguments);
			}

			public FieldInfo Field
			{
				get
				{
					return field;
				}
			}

			public override string ToString()
			{
				return base.ToString() + "->" + field.ToString();
			}
		}

		#endregion

		#region InlineVarInstruction

		public class InlineVarInstruction : ILInstruction
		{
			int index;

			public InlineVarInstruction(MethodBase enclosingMethod, int offset, OpCode opCode, int size, int index)
				: base(enclosingMethod, offset, opCode, size)
			{
				this.index = index;
			}

			public int Index
			{
				get
				{
					return index;
				}
			}

			public override string ToString()
			{
				return base.ToString() + "->" + index.ToString();
			}
		}

		#endregion

		#region BranchInstruction

		public class BranchInstruction : ILInstruction
		{
			int targetOffset;

			public BranchInstruction(MethodBase enclosingMethod, int offset, OpCode opCode, int size, int targetOffset)
				: base(enclosingMethod, offset, opCode, size)
			{
				this.targetOffset = targetOffset;
			}

			public int Target
			{
				get
				{
					return Next + targetOffset;
				}
			}

			public override string ToString()
			{
				return base.ToString() + "->" + Target.ToString();
			}
		}

		#endregion

		#region SwitchInstruction

		public class SwitchInstruction : ILInstruction
		{
			int[] cases;

			public SwitchInstruction(MethodBase enclosingMethod, int offset, OpCode opCode, int size, int[] cases)
				: base(enclosingMethod, offset, opCode, size)
			{
				this.cases = cases;
			}

			public int[] Cases
			{
				get
				{
					return cases;
				}
			}

			public override string ToString()
			{
				return base.ToString() + "->" + cases.Length.ToString();
			}
		}

		#endregion

		#region InlineStringInstruction

		public class InlineStringInstruction : ILInstruction
		{
			public string Value;

			public InlineStringInstruction(MethodBase enclosingMethod, int offset, OpCode opCode, int size, int token)
				: base(enclosingMethod, offset, opCode, size)
			{
				this.Value = enclosingMethod.DeclaringType.Assembly.ManifestModule.ResolveString(token);
			}
		}

		#endregion

	}

	#endregion

}
