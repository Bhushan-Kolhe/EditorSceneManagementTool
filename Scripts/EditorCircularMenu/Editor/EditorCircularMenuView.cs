using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Essentials.EditorCircularMenu
{
    public class EditorCircularMenuView
    {
        public EditorCircularMenuView Parent;
        public readonly string Path;
        public readonly string Icon;

        public readonly List<EditorCircularMenuView> Children = new();
        public readonly Action OnRadialMenuItemSelected;

        public EditorCircularMenuView(string path, string icon = null, Action onRadialMenuItemSelected = null, EditorCircularMenuView parent = null)
        {
            Path = path;
            Icon = icon;
            OnRadialMenuItemSelected = onRadialMenuItemSelected;
            Parent = parent;
        }
    }
}
