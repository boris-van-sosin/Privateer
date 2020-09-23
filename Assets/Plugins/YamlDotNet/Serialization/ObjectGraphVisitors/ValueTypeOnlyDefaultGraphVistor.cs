using System;
using System.Collections.Generic;
using System.ComponentModel;
using YamlDotNet;
using YamlDotNet.Core;

namespace YamlDotNet.Serialization.ObjectGraphVisitors
{
    public sealed class ValueTypeOnlyDefaultGraphVistor : ChainedObjectGraphVisitor
    {
        public ValueTypeOnlyDefaultGraphVistor(IObjectGraphVisitor<IEmitter> nextVisitor)
            : base(nextVisitor)
        {
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType() ? Activator.CreateInstance(type) : null;
        }

        private static readonly IEqualityComparer<object> _objectComparer = EqualityComparer<object>.Default;

        public override bool EnterMapping(IObjectDescriptor key, IObjectDescriptor value, IEmitter context)
        {
            return (value.Type.IsValueType() || !_objectComparer.Equals(value, GetDefault(value.Type)))
                   && base.EnterMapping(key, value, context);
        }

        public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context)
        {
            var defaultValueAttribute = key.GetCustomAttribute<DefaultValueAttribute>();
            var defaultValue = defaultValueAttribute != null
                ? defaultValueAttribute.Value
                : GetDefault(key.Type);

            return (value.Type.IsValueType() || !_objectComparer.Equals(value.Value, defaultValue))
                   && base.EnterMapping(key, value, context);
        }
    }
}
