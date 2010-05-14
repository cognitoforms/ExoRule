using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ExoRule
{
	[DataContract]
	public abstract class Permission : ConditionType
	{
		public Permission(string defaultMessage, PermissionType permissionType)
			: this(null, defaultMessage, permissionType)
		{ }

		public Permission(string code, string defaultMessage, PermissionType permissionType)
			: base(code, ConditionCategory.Permission, defaultMessage)
		{
			PermissionType = permissionType;
		}

		public PermissionType PermissionType { get; private set; }

		[DataMember(Name = "isAllowed")]
		public abstract bool IsAllowed { get; protected set; }
	}

	[DataContract]
	public class DenyPermission : Permission 
	{
		public DenyPermission(string defaultMessage, PermissionType permissionType)
			: this(null, defaultMessage, permissionType)
		{ }

		public DenyPermission(string code, string defaultMessage, PermissionType permissionType)
			: base(code, defaultMessage, permissionType)
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
		public DenyPermission(string defaultMessage, PermissionType permissionType, Predicate<TRoot> condition)
			: this(null, defaultMessage, permissionType, condition)
		{ }

		public DenyPermission(string code, string defaultMessage, PermissionType permissionType, Predicate<TRoot> condition)
			: base(code, defaultMessage, permissionType)
		{
			CreateConditionRule<TRoot>(condition, null, null);
		}
	}
}
