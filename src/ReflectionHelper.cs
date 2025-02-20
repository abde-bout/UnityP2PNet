using System.Linq;
using System.Reflection;
using System;
using P2PNet;
using P2PNet.Serialization;

namespace UnityP2PNet
{
    public static class ReflectionHelper
    {
        public static object[] DeserializeParameters(MethodInfo methodInfo, Serializer serializer)
        {
            var paramInfo = methodInfo.GetParameters();

            object[] deserializedParameters = new object[paramInfo.Length];

            for (int i = 0; i < paramInfo.Length; i++)
            {
                deserializedParameters[i] = serializer.GetNextItem(paramInfo[i].ParameterType);
            }

            return deserializedParameters;
        }
    }
}