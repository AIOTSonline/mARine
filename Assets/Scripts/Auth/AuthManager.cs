using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class AuthManager : MonoBehaviour
{
    private FirebaseAuth auth;

    [Header("Login")]
    public TMP_InputField loginEmailInput;
    public TMP_InputField loginPasswordInput;

    [Header("Register")]
    public TMP_InputField registerNameInput;
    public TMP_InputField registerEmailInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField registerConfirmPasswordInput;

    [Header("Forgot Password")]
    public TMP_InputField forgotPasswordEmailInput;

    [Header("Status")]
    public TextMeshProUGUI statusText;

    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;

                // Uncomment later for persistent sessions
                /*
                if (auth.CurrentUser != null)
                {
                    SceneManager.LoadScene("StartScene");
                }
                */
            }
            else
            {
                statusText.text = "Firebase initialization failed.";
                Debug.LogError("Firebase dependency error: " + task.Result);
            }
        });
    }

    public void GuestLogin()
    {
        if (auth == null) return;

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                SceneManager.LoadScene("StartScene");
            }
            else
            {
                statusText.text = "Guest login failed.";
                Debug.LogError(task.Exception);
            }
        });
    }

    public void EmailLogin()
    {
        if (auth == null) return;

        auth.SignInWithEmailAndPasswordAsync(
            loginEmailInput.text.Trim(),
            loginPasswordInput.text
        ).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                SceneManager.LoadScene("StartScene");
            }
            else
            {
                statusText.text = "Login failed.";
                Debug.LogError(task.Exception);
            }
        });
    }

    public void Register()
    {
        if (auth == null) return;

        if (registerPasswordInput.text != registerConfirmPasswordInput.text)
        {
            statusText.text = "Passwords do not match.";
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(
            registerEmailInput.text.Trim(),
            registerPasswordInput.text
        ).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                statusText.text = "Account created.";
                SceneManager.LoadScene("StartScene");
            }
            else
            {
                statusText.text = "Registration failed.";
                Debug.LogError(task.Exception);
            }
        });
    }

    public void ResetPassword()
    {
        if (auth == null) return;

        auth.SendPasswordResetEmailAsync(
            forgotPasswordEmailInput.text.Trim()
        ).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                statusText.text = "Reset email sent.";
            }
            else
            {
                Debug.LogError(task.Exception);
                statusText.text = "Password reset failed.";
            }
        });
    }

    // Placeholder until Android-native Google Sign-In is implemented
    public void GoogleLoginPlaceholder()
    {
        statusText.text = "Google Sign-In available in Android build.";
    }
}