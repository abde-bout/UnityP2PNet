using P2PNet;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityP2PNet
{
    [CreateAssetMenu(fileName = "Net Settings", menuName = "Data/Net/Settings")]
    public class NetSettingsData : ScriptableObject
    {
        static readonly string[] ignoreAssemblies = new string[]
        {
            "System", "UnityEngine", "Unity", "UnityEditor"
        };

        public int MaxGameObjects => maxGameObject;
        public GameObject[] NetPrefabs => netPrefabs;
        public List<MethodMetadata> NetMethodsMetaData => netMethodsMetadata;

        [Header("Settings")]
        [SerializeField] string[] targetAssemblies = new string[] { "Assembly-CSharp" };
        [SerializeField] int maxGameObject = 1000;
        [SerializeField] GameObject[] netPrefabs;
        [SerializeField] List<MethodMetadata> netMethodsMetadata;

#if UNITY_EDITOR
        public void Refresh()
        {
            Debug.Log("===(Linqing shared monos)===");
            LinqSharedMonos();
            Debug.Log("===(Linqing prefabs net identifiers)===");
            LinqNetIdentifiers();
            Debug.Log("===(Updating sync methods)===");
            UpdateSyncMethods();
        }

        void LinqSharedMonos()
        {
            var initialScenePath = SceneManager.GetActiveScene().path;
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            var sharedId = 0;
            for (int i = 0; i < sceneCount; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var loadedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                foreach (GameObject rootObj in loadedScene.GetRootGameObjects())
                {
                    var netIdentifier = rootObj.GetComponent<NetIdentifier>();
                    netIdentifier?.LinqNetMonos();
                    var sharedMonos = rootObj.GetComponentsInChildren<NetSharedMonoBehaviour>();
                    foreach (var sharedMono in sharedMonos) sharedMono.SetSharedId(sharedId++);
                }
                EditorSceneManager.MarkSceneDirty(loadedScene);
                EditorSceneManager.SaveScene(loadedScene);
            }
            Debug.Log($"Assigned {(sharedId)} shared-ids");
            EditorSceneManager.OpenScene(initialScenePath, OpenSceneMode.Single);
        }

        void LinqNetIdentifiers()
        {
            foreach (var prefab in netPrefabs)
            {
                prefab.GetComponent<NetIdentifier>().LinqNetMonos();
                EditorUtility.SetDirty(prefab);
            }
            AssetDatabase.SaveAssets();
        }

        void UpdateSyncMethods()
        {
            netMethodsMetadata.Clear();

            var instanceCount = 0;
            var methodMetadataBag = new ConcurrentBag<MethodMetadata>();

            var assemblies = targetAssemblies.Length > 0 ?
                targetAssemblies.Select(assembly => Assembly.Load(assembly)).ToArray() :
                AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !ignoreAssemblies.Any(ignore => assembly.FullName.StartsWith(ignore)));

            Parallel.ForEach(assemblies, assembly =>
            {
                try
                {
                    Debug.Log(assembly.FullName);

                    // Get all class types in the assembly
                    var classTypes = assembly.GetTypes().Where(t => t.IsClass);

                    foreach (var type in classTypes)
                    {
                        // Get methods defined only in this class
                        var methodsInfo = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                        foreach (var methodInfo in methodsInfo)
                        {
                            var attribute = methodInfo.GetCustomAttribute<SyncAttribute>(false);

                            if (attribute != null)
                            {
                                methodMetadataBag.Add(new MethodMetadata
                                {
                                    DeclaringType = methodInfo.DeclaringType.FullName,
                                    MethodName = methodInfo.Name,
                                    ParameterTypes = methodInfo.GetParameters().Select(p => p.ParameterType.FullName).ToList(),
                                    Protocole = attribute.Protocole,
                                    BroadcastPermission = attribute.BroadcastPermission,
                                });

                                Interlocked.Increment(ref instanceCount);
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Debug.LogWarning($"Skipped assembly {assembly.FullName} due to reflection errors: {ex}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Fatal exception processing assembly {assembly.FullName}: {ex}");
                }
            });

            // Convert to a list and sort
            netMethodsMetadata = methodMetadataBag.ToList();
            netMethodsMetadata.Sort((metal, metar) => metal.MethodName.CompareTo(metar.MethodName));

            // Update asset
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            Debug.Log($"Found {instanceCount} tagged methods");
        }

#endif
        public List<MethodInfo> ResolveNetMethods()
        {
            var resolvedMethods = new List<MethodInfo>();

            foreach (var metadata in netMethodsMetadata)
            {
                var type = Type.GetType(metadata.DeclaringType);
                if (type != null)
                {
                    var method = type.GetMethod(
                        metadata.MethodName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        metadata.ParameterTypes.Select(s => Type.GetType(s)).ToArray(),
                        null
                    );

                    if (method != null)
                    {
                        resolvedMethods.Add(method);
                    }
                }
            }

            return resolvedMethods;
        }

        private void OnValidate()
        {
            foreach (var prefab in netPrefabs)
            {
                if (!prefab.TryGetComponent<NetIdentifier>(out _))
                {
                    throw new NullReferenceException("Prefab does not contain a " + typeof(NetIdentifier));
                }
            }
        }

        [Serializable]
        public class MethodMetadata
        {
            public string MethodName;
            public string DeclaringType;
            public ProtocolType Protocole;
            public BroadcastPermission BroadcastPermission;
            public List<string> ParameterTypes;
        }
    }
}

