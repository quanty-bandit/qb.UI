using qb.Cache.Network;
using System;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;
using qb.Threading;

#if UNITY_EDITOR
using TriInspector;
using UnityEditor;
#endif


namespace qb.Test
{
    public class WebImageTest : MonoBehaviour
    {
        [SerializeField]
        string picsumRequestUrl = "https://picsum.photos/v2/list?limit={0}";
        [SerializeField]
        string gifyRequestUrl = "https://api.giphy.com/v1/gifs/search?api_key=Il3tIeXJMYZcTdPlAtY679Hi0wpyPw9J&q=twitch+emotes&limit={0}&offset=0&rating=g&lang=en&bundle=messaging_non_clips";
        
        [SerializeField,Min(1)]
        int imageCount = 100;

#if UNITY_EDITOR
#if !UNITY_WEBGL && !UNITY_WEBGL_API
        [Button]
        void CopyCacheDirPath()
        {
            var path = WebTextureCacheHandler.GetCacheDirPath();
            Debug.Log($"<color=#00FFFF>{path}</color>");
            EditorGUIUtility.systemCopyBuffer = path;
        }
        [Button]
        void ClearCacheFromDisk()
        {
            Debug.Log($"<color=#FFFF00>Remove dir: {WebTextureCacheHandler.GetCacheDirPath()}</color>");
            WebTextureCacheHandler.DeleteAllSavedCache();
        }
#endif
#endif
        [Serializable]
        public class PicSumData
        {
            public string download_url;
        }
        [Serializable]
        public class GifyData
        {
            [Serializable]
            public class Data
            {
                public string type;
                public string id;
                [Serializable]
                public class ImagesData
                {
                    public class ImageData
                    {
                        public string width, height;
                        public string url;
                    }
                    public ImageData original;
                }
                public ImagesData images;
            }
            public Data[] data;
        }

        async void Start()
        {
            await Task.Yield();
            int count = Mathf.RoundToInt( imageCount/2f );
            var mainThreadAction = MainThreadAction.GetInstance<MainThreadAction>(true);
            using (UnityWebRequest uwr = UnityWebRequest.Get(string.Format(picsumRequestUrl, count)))
            {
                var op  = uwr.SendWebRequest();
                while (!op.isDone)
                {
                    await Task.Yield ();
                }
                if(uwr.result == UnityWebRequest.Result.Success)
                {
                    
                    var jsonString = uwr.downloadHandler.text;
                    var datas  = Utility.WebRequestUtility.UserializeFromJsonString<PicSumData[]>(jsonString);
                    if(datas != null)
                    {
                        Debug.Log("First load!!!");
                        foreach(var entry in datas)
                        {
                            Debug.Log(entry.download_url);
                            
                            var handler = WebTextureCacheHandler.Get(mainThreadAction,entry.download_url);
                            if (handler == null) continue;
                            _= handler.LoadRequest(this);
                            Debug.Log($"{WebTextureCacheHandler.TotalCacheSize} / {WebTextureCacheHandler.GetMaxCacheSizeInByte()} : {WebTextureCacheHandler.MemoryFillRate * 100}%");
                            //handler.Release(this);
                            await Task.Yield();
#if UNITY_EDITOR
                            if (!EditorApplication.isPlaying)
                                return;
#endif
                        }
                        foreach (var entry in datas)
                        {
                            var handler = WebTextureCacheHandler.Get(mainThreadAction, entry.download_url);
                            if (handler != null)
                                handler.Release(this);
                        }
                        Debug.Log("Second load from cache!!!");
                        foreach (var entry in datas)
                        {
                            Debug.Log(entry.download_url);

                            var handler = WebTextureCacheHandler.Get(mainThreadAction,entry.download_url, WebTextureCacheHandler.EFormat.bin);
                            if (handler == null) continue;
                            _= handler.LoadRequest(this,cacheSizeTest: WebTextureCacheHandler.ECacheSizeTest.MatchCacheSize);
                            Debug.Log($"<color=#00FFFF>{WebTextureCacheHandler.TotalCacheSize} / {WebTextureCacheHandler.GetMaxCacheSizeInByte()} : {WebTextureCacheHandler.MemoryFillRate * 100}%</color>");
                            //handler.Release(this);
                            await Task.Yield();
#if UNITY_EDITOR
                            if (!EditorApplication.isPlaying)
                                return;
#endif
                        }
                        foreach (var entry in datas)
                        {
                            var handler = WebTextureCacheHandler.Get(mainThreadAction, entry.download_url,WebTextureCacheHandler.EFormat.bin);
                            if (handler != null)
                                handler.Release(this);
                        }
                    }
                }
            }

            using (UnityWebRequest uwr = UnityWebRequest.Get(string.Format(gifyRequestUrl, count)))
            {
                var op = uwr.SendWebRequest();
                while (!op.isDone)
                {
                    await Task.Yield();
                }
                if (uwr.result == UnityWebRequest.Result.Success)
                {

                    var jsonString = uwr.downloadHandler.text;
                    var datas = Utility.WebRequestUtility.UserializeFromJsonString<GifyData>(jsonString);
                    if (datas != null)
                    {
                        Debug.Log("First load!!!");
                        foreach (var entry in datas.data)
                        {
                            var url = entry.images.original.url;
                            Debug.Log(url);

                            var handler = WebTextureCacheHandler.Get(mainThreadAction, url, WebTextureCacheHandler.EFormat.gif,6);
                            if (handler == null) continue;
                            _= handler.LoadRequest(this);
                            Debug.Log($"{WebTextureCacheHandler.TotalCacheSize} / {WebTextureCacheHandler.GetMaxCacheSizeInByte()} : {WebTextureCacheHandler.MemoryFillRate * 100}%");
                            //handler.Release(this);
                            await Task.Yield();
#if UNITY_EDITOR
                            if (!EditorApplication.isPlaying)
                                return;
#endif
                        }
                        foreach (var entry in datas.data)
                        {
                            var url = entry.images.original.url;
                            var handler = WebTextureCacheHandler.Get(mainThreadAction,url, WebTextureCacheHandler.EFormat.gif);
                            if (handler != null)
                                handler.Release(this);
                        }
                        Debug.Log("Second load from cache!!!");
                        foreach (var entry in datas.data)
                        {
                            var url = entry.images.original.url;
                            Debug.Log(url);

                            var handler = WebTextureCacheHandler.Get(mainThreadAction, url, WebTextureCacheHandler.EFormat.gif);
                            if (handler == null) continue;
                            await handler.LoadRequest(this, cacheSizeTest: WebTextureCacheHandler.ECacheSizeTest.MatchCacheSize);
                            Debug.Log($"<color=#00FFFF>{WebTextureCacheHandler.TotalCacheSize} / {WebTextureCacheHandler.GetMaxCacheSizeInByte()} : {WebTextureCacheHandler.MemoryFillRate * 100}%</color>");
                            //handler.Release(this);
                            await Task.Yield();
#if UNITY_EDITOR
                            if (!EditorApplication.isPlaying)
                                return;
#endif
                        }
                    }
                }
            }


            WebTextureCacheHandler.DisposeUnusedTextures();
            Debug.Log($"<color=#FFFFFF>{WebTextureCacheHandler.TotalCacheSize} / {WebTextureCacheHandler.GetMaxCacheSizeInByte()} : {WebTextureCacheHandler.MemoryFillRate * 100}%</color>");
        }

    }
}
