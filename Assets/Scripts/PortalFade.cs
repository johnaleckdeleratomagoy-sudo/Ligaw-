using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalFade : MonoBehaviour
{
    public Animator fadeAnimator;
    public string sceneToLoad;

    public void FadeAndLoad()
    {
        StartCoroutine(Fade());
    }

    System.Collections.IEnumerator Fade()
    {
        fadeAnimator.SetTrigger("FadeOut");

        yield return new WaitForSeconds(0.6f);

        SceneManager.LoadScene(sceneToLoad);
    }
}