using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ExoRule
{
	public abstract class Permission : ConditionType
	{
		public Permission(string message, PermissionType permissionType, params ConditionTypeSet[] sets)
			: this(null, message, permissionType, sets)
		{ }

		public Permission(string code, string message, PermissionType permissionType, params ConditionTypeSet[] sets)
			: base(code, ConditionCategory.Permission, message, sets)
		{
			PermissionType = permissionType;
		}

		public Permission(string code, string message, Type sourceType, Func<string, string> translator, params ConditionTypeSet[] sets)
			: base(code, ConditionCategory.Permission, message, sourceType, translator, sets)
		{ }

		string PermissionTypeString
		{
			get
			{
				return PermissionType.ToString();
			}
			set { }
		}

		public PermissionType PermissionType { get; private set; }

		public abstract bool IsAllowed { get; protected set; }
	}

	public class DenyPermission : Permission 
	{
		public DenyPermission(string message, PermissionType permissionType)
			: this(null, message, permissionType)
		{ }

		public DenyPermission(string code, string message, PermissionType permissionType)
			: base(code, message, permissionType)
		{ }

		public override bool IsAllowed
		{
			get
			{
				return false;
			}
			protected set
			{ }
		}
	}

	public class DenyPermission<TRoot> : DenyPermission
		where TRoot : class
	{
		public DenyPermission(string message, PermissionType permissionType, Predicate<TRoot> condition)
			: this(null, message, permissionType, condition)
		{ }

		public DenyPermission(string code, string message, PermissionType permissionType, Predicate<TRoot> condition)
			: base(code, message, permissionType)
		{
			CreateConditionRule<TRoot>(condition, null, null);
		}
	}
}
