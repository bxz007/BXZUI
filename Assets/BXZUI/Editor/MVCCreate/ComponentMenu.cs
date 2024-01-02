using UnityEditor;
using UnityEngine;

namespace GalaFramework
{
    public class ComponentMenuWindow : EditorWindow {
        private static ComponentMenuWindow _window;


        public static void Open (MVCTreeItem TreeItem,Rect rect) {
            if (_window != null) {
                return;
            }
            _window = EditorWindow.GetWindow<ComponentMenuWindow> (false, "编辑器说明");
            
            Rect rectx = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y,rect.width,rect.height);
            _window.position = rectx;
        }

        public void OnLostFocus() {
            Hide();
	    }

        public static void Hide()
        {
            if(_window != null)
            {
                _window.Close();
            }
            _window = null;
        }

        private Vector2 _vect2;

        public void OnGUI () {

        }
    }
}