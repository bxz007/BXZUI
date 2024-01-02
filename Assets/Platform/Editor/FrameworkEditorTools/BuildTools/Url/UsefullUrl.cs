using System.Net.Mime;
using UnityEditor;
using UnityEngine;


namespace Gala.FrameworkEditorTools
{
    public class UsefullUrl
    {
        private const string URL_ZenTao = "http://zen.wckj.com/my/";
        private const string URL_ZenTao_MyBug = "http://zen.wckj.com/my-bug-assignedTo.html";

        public static void OpenZenTao()
        {
            Application.OpenURL(URL_ZenTao);
        }

        public static void OpenZenTaoMyBug()
        {
            Application.OpenURL(URL_ZenTao_MyBug);
        }

        public static void OpenUrl(string str)
        {
            Application.OpenURL(str);
        }

        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }

        public static void ClearEditorPrefs()
        {
            EditorPrefs.DeleteAll();
        }

        public static void OpenPersistentDataPath()
        {
            Application.OpenURL(Application.persistentDataPath);
        }
    }
}
