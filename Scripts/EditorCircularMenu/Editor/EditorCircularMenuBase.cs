using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Essentials.EditorCircularMenu
{
    public abstract class EditorCircularMenuBase
    {
        protected abstract KeyCode ActivationKey { get; }

        private SceneView _activeSceneView;
        private int _activeSceneViewInstanceID;

        private int _radius = 150;
        private VisualElement _menuRootElement;
        private VisualElement _sceneRootElement;
        private int _currentSection = 0;
        private int _hoveredSection = 0;
        protected EditorCircularMenuView _rootMenuView = new EditorCircularMenuView("root");
        protected EditorCircularMenuView _activeMenuView;

        protected Vector2 _mousePositionWhenMenuOpened;
        private Vector2 _currentMousePosition;
        private float _currentMouseAngle;

        private readonly Color AnnulusColor = new Color(0.02f, 0.02f, 0.02f, 0.8f);
        private readonly Color MouseAngleIndicatorBackgroundColor = new Color(0.01f, 0.01f, 0.01f, 1.0f);
        private readonly Color MouseAngleIndicatorForegroundColor = Color.white;

        private bool _isMenuVisible = false;
        private bool _wasKeyReleased = true;
        private bool _isInitialized = false;

        protected abstract void CreateMenu();

        protected void OnEditorApplicationUpdate()
        {
            _activeSceneView = SceneView.lastActiveSceneView;

            if (_activeSceneView == null) return;

            if (_activeSceneView.GetInstanceID() != _activeSceneViewInstanceID)
            {
                _activeSceneViewInstanceID = _activeSceneView.GetInstanceID();
                RemovePreviousMenu();

                _sceneRootElement = _activeSceneView.rootVisualElement;

                if (_menuRootElement == null)
                    InitializeMenu();
            }
        }

        protected void ReInitializeMenu()
        {
            if (_isInitialized)
            {
                InitializeMenu();
            }
        }

        private void InitializeMenu()
        {
            if (_menuRootElement is { }) RemovePreviousMenu();

            if (_rootMenuView == null) _rootMenuView = new EditorCircularMenuView("root");


            // Create the root VisualElement that holds the radial menu.
            _menuRootElement = new VisualElement
            {
                style = {
                    position = Position.Absolute,
                    width = _radius,
                    height = _radius,
                    display = DisplayStyle.Flex,
                    marginBottom = 0.0f,
                    marginTop = 0.0f,
                    marginRight = 0.0f,
                    marginLeft = 0.0f,
                    paddingBottom = 0.0f,
                    paddingTop = 0.0f,
                    paddingRight = 0.0f,
                    paddingLeft = 0.0f,
                    alignItems = Align.Center,
                    alignContent = Align.Center,
                    justifyContent = Justify.Center,
                }
            };

            // Draw the center mouse angle indicator.
            _menuRootElement.generateVisualContent -= DrawMouseAngleIndicator;
            _menuRootElement.generateVisualContent += DrawMouseAngleIndicator;

            _rootMenuView.Children.Clear();

            CreateMenu();

            _activeMenuView = _rootMenuView;

            // Add the radial menu root to the scene view root.
            _sceneRootElement.Add(_menuRootElement);

            HideMenu();
            _isInitialized = true;
        }

        protected void ShowMenu(Vector2 position)
        {
            if (_menuRootElement is null) return;
            _menuRootElement.SetEnabled(true);
            _menuRootElement.style.display = DisplayStyle.Flex;
            _isMenuVisible = true;
            _menuRootElement.transform.position = position - new Vector2(_radius * 0.5f, _radius * 0.5f);
            RebuildMenu();
            _currentSection = 0;
            List<EditorCircularMenuButton> buttons = _menuRootElement.Children().Where(child => child is EditorCircularMenuButton).ToList().Select(e => e as EditorCircularMenuButton).ToList();
            buttons[_currentSection].Hover(true);
            _activeSceneView.Repaint();
        }

        protected void HideMenu()
        {
            _menuRootElement.SetEnabled(false);
            _menuRootElement.style.display = DisplayStyle.None;
            _isMenuVisible = false;
            _activeMenuView = _rootMenuView;
            _currentSection = 0;
        }

        private void RebuildMenu()
        {
            _menuRootElement.Clear();

            if (_activeMenuView.Parent != null)
            {
                _menuRootElement.Add(new Label(_activeMenuView.Path)
                {
                    style =
                    {
                        marginBottom = _radius * 0.5f + 5.0f,
                        fontSize = 10,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        color = Color.white,
                        textShadow = new TextShadow
                        {
                            offset = new Vector2(0.2f, 0.2f),
                            blurRadius = 0,
                            color = Color.black
                        }
                    }
                });

                // Back Button
                _menuRootElement.Add(new EditorCircularMenuButton("Back", "", 0, () => SelectMenuItem(_activeMenuView.Parent)));
            }
            else
            {
                _menuRootElement.Add(new Label(""));
            }

            int section = 1;

            foreach (var item in _activeMenuView.Children)
            {
                _menuRootElement.Add(new EditorCircularMenuButton(
                    item.Children.Count > 0 ? item.Path + "" : item.Path,
                    item.Icon,
                    section,
                    item.OnRadialMenuItemSelected));
                section++;
            }

            // Move all buttons outwards using an animation.
            var i = 0;
            foreach (var item in _menuRootElement.Children().Where(c => c is EditorCircularMenuButton))
            {
                item.transform.position = Vector3.zero;
                var targetPosition = Vector2.zero + GetCircleOffset(_radius, i, _menuRootElement.childCount - 1);
                item.experimental.animation.Position(targetPosition, 100);
                i++;
            }
        }

        protected void OnDuringSceneGUI(SceneView view)
        {
            Event currentEvent = Event.current;

            switch (currentEvent.type)
            {
                case EventType.Repaint:
                    _currentMousePosition = currentEvent.mousePosition;
                    break;
                case EventType.KeyDown when (currentEvent.keyCode == ActivationKey && !_isMenuVisible && _wasKeyReleased):
                    _wasKeyReleased = false;
                    _mousePositionWhenMenuOpened = currentEvent.mousePosition;
                    ShowMenu(_mousePositionWhenMenuOpened);
                    break;
                case EventType.MouseMove when _isMenuVisible:
                    RecalculateCurrentAndHoveredSection();
                    _menuRootElement.MarkDirtyRepaint();
                    break;
                case EventType.KeyUp when (currentEvent.keyCode == ActivationKey && !_isMenuVisible):
                    _wasKeyReleased = true;
                    break;
                case EventType.KeyUp when (currentEvent.keyCode == ActivationKey && _isMenuVisible):
                    _wasKeyReleased = true;
                    HideMenu();
                    break;
                case EventType.MouseDown when (currentEvent.button == 0 && _isMenuVisible):
                    if (_activeMenuView.Parent != null)
                    {
                        if (_currentSection == 0)
                        {
                            // Select Back Button
                            SelectMenuItem(_activeMenuView.Parent);
                        }
                        else
                        {
                            SelectMenuItem(_currentSection - 1);
                        }
                    }
                    else
                    {
                        SelectMenuItem(_currentSection);
                    }
                    break;
            }
        }

        private void RecalculateCurrentAndHoveredSection(bool menuRebuilt = false)
        {
            int sectionCount = _activeMenuView.Parent == null ? _activeMenuView.Children.Count : _activeMenuView.Children.Count + 1;
            float sectionPartAngle = 360.0f / sectionCount;
            Vector2 mouseVector = (_currentMousePosition - _mousePositionWhenMenuOpened).normalized;
            float angle = (float)((Math.Atan2(mouseVector.y, mouseVector.x) * Mathf.Rad2Deg));
            _currentMouseAngle = angle < 0 ? angle + 360 : angle;

            angle = (angle + (sectionPartAngle * 0.5f));
            angle = angle < 0 ? angle + 360 : angle % 360;
            _hoveredSection = (int)(angle / sectionPartAngle);


            if (_currentSection != _hoveredSection || menuRebuilt)
            {
                List<EditorCircularMenuButton> buttons = _menuRootElement.Children().Where(child => child is EditorCircularMenuButton).ToList().Select(e => e as EditorCircularMenuButton).ToList();
                if (_currentSection < buttons.Count)
                    buttons[_currentSection].Hover(false);
                buttons[_hoveredSection].Hover(true);
                _currentSection = _hoveredSection;
            }
        }

        private void RemovePreviousMenu()
        {
            if (_rootMenuView is null || _menuRootElement is null) return;
            _menuRootElement.RemoveFromHierarchy();
            _rootMenuView = null;
        }

        private Vector2 GetCircleOffset(float radius, float index, float numberOfSections)
        {
            var angle = (360.0f / numberOfSections) * index;
            var offset = new Vector2
            {
                y = radius * Mathf.Sin(angle * Mathf.Deg2Rad),
                x = radius * Mathf.Cos(angle * Mathf.Deg2Rad),
            };
            return offset;
        }

        private void DrawMouseAngleIndicator(MeshGenerationContext context)
        {
            var position = new Vector2(_radius * 0.5f, _radius * 0.5f);
            var radius = _radius * 0.1f;
            const float indicatorSizeDegrees = 60.0f;

            var painter = context.painter2D;
            painter.lineCap = LineCap.Butt;

            // Draw the annulus.
            painter.lineWidth = 8.0f;
            painter.strokeColor = AnnulusColor;
            painter.BeginPath();
            painter.Arc(new Vector2(position.x, position.y), radius, 0.0f, 360.0f);
            painter.Stroke();

            // Draw the mouse angle indicator background.
            painter.lineWidth = 8.0f;
            painter.strokeColor = MouseAngleIndicatorBackgroundColor;
            painter.BeginPath();
            painter.Arc(new Vector2(position.x, position.y), radius, _currentMouseAngle - indicatorSizeDegrees * 0.5f,
                _currentMouseAngle + indicatorSizeDegrees * 0.5f);
            painter.Stroke();

            // Draw the mouse angle indicator.
            painter.lineWidth = 4.0f;
            painter.strokeColor = MouseAngleIndicatorForegroundColor;
            painter.BeginPath();
            painter.Arc(new Vector2(position.x, position.y), radius, _currentMouseAngle - indicatorSizeDegrees * 0.5f,
                _currentMouseAngle + indicatorSizeDegrees * 0.5f);
            painter.Stroke();
        }

        protected void SelectMenuItem(EditorCircularMenuView circularMenuView)
        {
            _activeMenuView = circularMenuView;
            RebuildMenu();
            RecalculateCurrentAndHoveredSection(true);
        }

        protected void SelectMenuItem(int index)
        {
            _activeMenuView.Children[index].OnRadialMenuItemSelected();
        }
    }
}
