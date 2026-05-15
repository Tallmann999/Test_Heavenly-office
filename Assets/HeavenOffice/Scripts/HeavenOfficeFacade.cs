using UnityEngine;

namespace HeavenOffice
{
    public sealed class HeavenOfficeFacade : MonoBehaviour
    {
        [SerializeField] private HeavenOfficeSessionConfig sessionConfig;
        [SerializeField] private bool startOnAwake = true;

        private HeavenOfficeSessionController sessionController;
        private HeavenOfficeViewController viewController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<HeavenOfficeFacade>() != null)
            {
                return;
            }

            var facadeObject = new GameObject("Heaven Office Facade");
            facadeObject.AddComponent<HeavenOfficeFacade>();
        }

        private void Awake()
        {
            if (startOnAwake)
            {
                StartSession();
            }
        }

        public void StartSession()
        {
            if (sessionController != null)
            {
                return;
            }

            var config = sessionConfig != null ? sessionConfig : HeavenOfficeSessionConfig.CreateRuntimeDefault();
            sessionController = new HeavenOfficeSessionController(config, new DebugHeavenOfficeAnalytics());
            viewController = gameObject.AddComponent<HeavenOfficeViewController>();
            viewController.Initialize(sessionController);
            sessionController.StartSession();
        }

        public void StopSession()
        {
            sessionController?.ExitSession();
        }

        private void OnDestroy()
        {
            sessionController?.ExitSession();
        }
    }
}
