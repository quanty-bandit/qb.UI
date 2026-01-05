using Extensions.Texture2D_ToSprite;
using qb.Animation;
using qb.Cache.Network;
using qb.Threading;
using System.Threading;
using System.Threading.Tasks;
using TriInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace qb.UI
{
    public class WebImage : MonoBehaviour
    {
        [SerializeField,Required]
        private Image image;
        public Image Image => image;

        [SerializeField]
        private Image loader;
        [SerializeField,Min(0.1f)]
        private float minLoadingProgressDuration = 1f; 
        [SerializeField]
        private string url;
        [SerializeField]
        private bool loadOnStart = false;
#if UNITY_EDITOR
        void OnPreserveAspectChanged()
        {
            PreserveAspect = preserveAspect;
        }
        [OnValueChanged(nameof(OnPreserveAspectChanged))]
#endif
        [SerializeField]
        private bool preserveAspect = true;
        public bool PreserveAspect
        {
            get=>preserveAspect;
            set
            {
                preserveAspect = value;
                if (image != null)
                {
                    image.preserveAspect = value;
                }
            }
        }
        [SerializeField]
        bool disposeTextureIfNoMoreUsed = false;


       
#if UNITY_EDITOR
        void UpdatePlayMode()
        {
            if (indexAnimation!=null)
                indexAnimation.playMode = playMode;
        }
        [OnValueChanged(nameof(UpdatePlayMode))]
        [Header("Animation tweenSettings")]
#endif
        [SerializeField]
        LinearIndexAnimation.EPlayMode playMode = LinearIndexAnimation.EPlayMode.linear;

#if UNITY_EDITOR
        void UpdateSeep()
        {
            if (indexAnimation != null)
                indexAnimation.Speed = speed;
        }
        [OnValueChanged(nameof(UpdateSeep))]
#endif
        [SerializeField,Range(0.1f,10)]
        float speed = 1;

        [SerializeField]
        int maxImageCount = 0;

        [Header("Event")]
        public UnityEvent onLoadCompleted = new UnityEvent();
        public UnityEvent<string> onLoadFailed = new UnityEvent<string>();


        WebTextureCacheHandler textureHandler;
        CancellationTokenSource cancellationTokenSource;

        bool initialImageEnableState;
        LinearIndexAnimation indexAnimation;
        Sprite[] sprites;
        MainThreadAction mainThreadAction;
        bool isInitialized;
        private void Awake()
        {
            mainThreadAction = MainThreadAction.GetInstance<MainThreadAction>(true);
            Initialize();
        }
        private void Initialize()
        {
            if (image != null && !isInitialized)
            {
                initialImageEnableState = image.enabled;
                isInitialized = true;
            }

        }

        void Start()
        {
            if (image!=null && loadOnStart)
                LoadAsync(url);
        }
        public async Task Load(string url)
        {
            if (string.IsNullOrEmpty(url) || image==null) return;
            this.url = url;
            Initialize();
            image.enabled = initialImageEnableState;

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                while(cancellationTokenSource!=null)
                    await Task.Yield();
            }
            cancellationTokenSource = new CancellationTokenSource();    

            if (textureHandler != null)
            {
                textureHandler.Release(this, disposeTextureIfNoMoreUsed);
            }

            if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
                return;
            }

            textureHandler = WebTextureCacheHandler.Get(mainThreadAction, url,WebTextureCacheHandler.EFormat.unknown, maxImageCount);
            if (textureHandler == null)
            {
                return;
            }

            if (loader != null)
            {
                loader.fillAmount = 0;
                loader.gameObject.SetActive(true);
                float loadingDuration = 0;
                await textureHandler.LoadRequest(this,onProgress: async (p) =>
                {
                    if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                        return;
                    if (p == 0 || p == 1)
                    {
                        p = loader.fillAmount+(1 / minLoadingProgressDuration * Time.deltaTime);
                        if (p == 1) p = 0.95f;
                    }
                    loader.fillAmount = p;
                    loadingDuration += Time.deltaTime;
                    await Task.Yield();
                });
#if UNITY_EDITOR
                if (this == null) return; //fix to avoid error log on editor when player is stopped !
#endif

                loader.fillAmount = 1;
                loader.gameObject.SetActive(false);
            }
            else
            {
                await textureHandler.LoadRequest(this);
            }

            if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
                return;
            }
           
            if (textureHandler.State == WebTextureCacheHandler.EState.Loaded)
            {
                onLoadCompleted.Invoke();
                switch (textureHandler.Format)
                {
                    case WebTextureCacheHandler.EFormat.bin:
                        image.sprite = textureHandler.Texture.ToSprite();
                        break;
                    case WebTextureCacheHandler.EFormat.gif:
                        var atlas = textureHandler.Atlas;
                        int framesCount = atlas.FramesCount;
                        sprites = new Sprite[framesCount];
                        for(int i = 0; i < framesCount; i++)
                        {
                            sprites[i] = atlas.CreateSprite(i);    
                        }
                        image.sprite = sprites[0];
                        indexAnimation = new LinearIndexAnimation(textureHandler.Delays,playMode: playMode);
                        indexAnimation.OnUpdate += IndexAnimation_OnUpdate;
                        UpdatableManager.GetInstance<UpdatableManager>(true);//Create animation manager if needed!
                        indexAnimation.Play();
                        break;
                }
                
                image.preserveAspect = preserveAspect;
                image.enabled = true;
            }
            else
            {
                onLoadFailed.Invoke(textureHandler.Error);
                Debug.LogError($"[TextureHandler][{textureHandler.State}][{textureHandler?.Error}] {url}");
            }

            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        private void IndexAnimation_OnUpdate(LinearIndexAnimation sender,int index)
        {
            if (sender == indexAnimation)
                image.sprite = sprites[index];
            else
                sender.Dispose();
        }

        public async void LoadAsync(string url)
        {
            await Load(url);
        }
        public void Clear()
        {
            if (cancellationTokenSource != null)
                cancellationTokenSource.Cancel();
            if (textureHandler != null)
            {
                textureHandler.Release(this, disposeTextureIfNoMoreUsed);
                image.enabled = false;
            }

        }
        private void OnDestroy()
        {
            if (cancellationTokenSource != null)
                cancellationTokenSource.Cancel();
            if (textureHandler != null)
                textureHandler.Release(this);
        }

    }
}
