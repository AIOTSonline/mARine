using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class PermissionManager : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(RequestAll());
    }

    public static IEnumerator RequestAll()
    {
#if UNITY_ANDROID
        yield return RequestOne(Permission.Camera);
        yield return RequestOne(Permission.Microphone);
#else
        yield break;
#endif
    }

    public static async Task RequestAllAsync()
    {
#if UNITY_ANDROID
        await RequestOneAsync(Permission.Camera);
        await RequestOneAsync(Permission.Microphone);
#else
        await Task.CompletedTask;
#endif
    }

#if UNITY_ANDROID
    static IEnumerator RequestOne(string permission)
    {
        if (Permission.HasUserAuthorizedPermission(permission))
            yield break;

        bool resolved = false;
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ => resolved = true;
        callbacks.PermissionDenied += _ => resolved = true;
        callbacks.PermissionDeniedAndDontAskAgain += _ => resolved = true;

        Permission.RequestUserPermission(permission, callbacks);

        float timeout = Time.realtimeSinceStartup + 30f;
        while (!resolved && Time.realtimeSinceStartup < timeout)
            yield return null;
    }

    static async Task RequestOneAsync(string permission)
    {
        if (Permission.HasUserAuthorizedPermission(permission))
            return;

        var tcs = new TaskCompletionSource<bool>();
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ => tcs.TrySetResult(true);
        callbacks.PermissionDenied += _ => tcs.TrySetResult(false);
        callbacks.PermissionDeniedAndDontAskAgain += _ => tcs.TrySetResult(false);

        Permission.RequestUserPermission(permission, callbacks);

        var timeout = Task.Delay(30000);
        await Task.WhenAny(tcs.Task, timeout);
    }
#endif
}
