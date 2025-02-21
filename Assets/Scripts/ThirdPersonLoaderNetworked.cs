using System;
using Mirror;
using ReadyPlayerMe.Core;
using UnityEngine;

namespace ReadyPlayerMe.Samples.QuickStart
{
    public class ThirdPersonLoaderNetworked : NetworkBehaviour
    {
        private readonly Vector3 avatarPositionOffset = new Vector3(0, -0.08f, 0);
        
        [SyncVar(hook = nameof(OnAvatarUrlChanged))]
        private string avatarUrl;

        //[SerializeField]
        //[Tooltip("RPM avatar URL or shortcode to load")]
        //private string avatarUrl;
        private GameObject avatar;
        private AvatarObjectLoader avatarObjectLoader;
        [SerializeField]
        [Tooltip("Animator to use on loaded avatar")]
        private RuntimeAnimatorController animatorController;        
        [SerializeField]
        [Tooltip("Preview avatar to display until avatar loads. Will be destroyed after new avatar is loaded")]
        private GameObject previewAvatar;

        public event Action OnLoadComplete;

        private NetworkAnimator networkAnimator;


        private void Awake() 
        {
            avatarObjectLoader = new AvatarObjectLoader();
            avatarObjectLoader.OnCompleted += OnLoadCompleted;
            avatarObjectLoader.OnFailed += OnLoadFailed;
            networkAnimator = GetComponent<NetworkAnimator>();
        }
        private void Start()
        {                                                                        
            if (previewAvatar != null)
            {
                SetupAvatar(previewAvatar);
            }
          
            if (isLocalPlayer)
            {
                CmdSetAvatarUrl(GameManager.Instance.personaUrl);
            }            
        }       

        private void OnLoadFailed(object sender, FailureEventArgs args)
        {
            Debug.Log("Load failed: " + args.Message);

            OnLoadComplete?.Invoke();
        }

        private void OnLoadCompleted(object sender, CompletionEventArgs args)
        {            
            if (previewAvatar != null)
            {
                Destroy(previewAvatar);
                previewAvatar = null;
            }
            SetupAvatar(args.Avatar);            
            
            OnLoadComplete?.Invoke();            
        }

        private void SetupAvatar(GameObject targetAvatar)
        {
            if (avatar != null)
            {
                Destroy(avatar);
            }

            avatar = targetAvatar;
            // Re-parent and reset transforms
            avatar.transform.parent = transform;
            avatar.transform.localPosition = avatarPositionOffset;
            avatar.transform.localRotation = Quaternion.Euler(0, 0, 0);
            
            // Configure controller
            var controller = GetComponent<ThirdPersonControllerNetworked>();
            if (controller != null)
            {
                controller.Setup(avatar, animatorController);
            }

            var animator = avatar.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator not found.");
                return;
            }
            networkAnimator.animator = animator;


            if (isLocalPlayer)
            {
                GameManager.Instance.localPlayerPrefab = avatar.transform.parent.gameObject;                
            }
        }

        public void LoadAvatar(string url)
        {            
            //remove any leading or trailing spaces
            avatarUrl = url.Trim(' ');
            avatarObjectLoader.LoadAvatar(avatarUrl);
        }

        private void OnAvatarUrlChanged(string oldUrl, string newUrl)
        {            
            if (avatarObjectLoader == null)
            {
                Debug.LogWarning("avatarObjectLoader is not initialized yet. Skipping avatar load.");
                return;
            }

            if (!string.IsNullOrEmpty(newUrl))
            {
                LoadAvatar(newUrl);
            }
        }

        [Command]
        private void CmdSetAvatarUrl(string url)
        {
            // Set the url on the server
            avatarUrl = url.Trim();
            LoadAvatar(avatarUrl);
        }


    }
}
