using AbdeUnity.Miscs;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityP2PNet.NetSettingsData;

namespace UnityP2PNet
{
    public class NetSettingsDataCache : ScriptableObjectCache
    {
        Dictionary<GameObject, int> _netPrefabMap;
        Dictionary<string, MethodInfo> _methodInfoMap;
        Dictionary<MethodInfo, int> _methodInfoIndexMap;
        List<MethodInfo> _methodsInfo;

        public NetSettingsDataCache(NetSettingsData source) : base(source)
        {
            _methodInfoMap = new();
            _methodInfoIndexMap = new();

            int index = 0;
            _methodsInfo = source.ResolveNetMethods();
            foreach (var methodInfo in _methodsInfo)
            {
                _methodInfoMap.Add(methodInfo.DeclaringType.FullName + methodInfo.Name + string.Join("", methodInfo.GetParameters().Select(p => p.ParameterType.FullName)), methodInfo);
                _methodInfoIndexMap.Add(methodInfo, index++);
            }

            _netPrefabMap = new();
            index = 0;
            foreach (var prefab in source.NetPrefabs)
            {
                _netPrefabMap.Add(prefab, index++);
            }
        }

        public int GetPrefabIndex(GameObject prefab) => _netPrefabMap[prefab];
        public GameObject GetPrefab(int index) => (Source as NetSettingsData).NetPrefabs[index];
        public int GetMethodInfoIndex(MethodInfo methodInfo)
        {
            if (_methodInfoIndexMap.TryGetValue(methodInfo, out var index))
            {
                return index;
            }
            else
            {
                throw new KeyNotFoundException($"MethodInfo index could not be found {methodInfo}.");
            }
        }
        public MethodInfo GetMethodInfo(int index)
        {
            if (index < 0 || index >= _methodsInfo.Count)
            {
                throw new System.IndexOutOfRangeException($"MethodInfo index {index} out of range");
            }

            return _methodsInfo[index];
        }
        public MethodInfo GetMethodInfo(object obj, string methodName, object[] parameters)
        {
            string key;
            var type = obj.GetType();

            string suffix = methodName + string.Join("", parameters.Select(p => p.GetType().FullName));

            while (type != null)
            {
                key = type.FullName + suffix;

                if (_methodInfoMap.TryGetValue(key, out var methodInfo))
                {
                    return methodInfo;
                }

                type = type.BaseType;
            }

            throw new KeyNotFoundException($"Method '{methodName}' with parameters_count {parameters.Length}" +
                $" could not be found on object '{obj}'\n" +
                $"1. Method signature mismatch\n" +
                $"2. It is missing the [{nameof(SyncAttribute)}] attribute)\n" +
                $"3. It is not registered in the {nameof(NetSettingsData)}");
        }
        public MethodMetadata GetMethodMetadata(MethodInfo methodInfo)
        {
            return GetMethodMetadata(GetMethodInfoIndex(methodInfo));
        }
        public MethodMetadata GetMethodMetadata(int index)
        {
            return (Source as NetSettingsData).NetMethodsMetaData[index];
        }
    }
}