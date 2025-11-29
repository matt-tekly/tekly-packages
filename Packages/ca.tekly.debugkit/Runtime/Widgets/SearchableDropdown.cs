using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tekly.DebugKit.Widgets
{
    public class SearchableDropdown<T> : VisualElement
    {
        public T Value
        {
            get => m_value;
            set => SetValueInternal(value, false);
        }
        
        public event Action<T> SelectionChanged;
        
        private readonly Label m_selectedLabel;

        private TextField m_searchField;
        private ListView m_listView;

        private VisualElement m_overlay;
        private VisualElement m_anchor;
        private VisualElement m_popup;

        private readonly List<T> m_allItems = new List<T>();
        private readonly List<T> m_filteredItems = new List<T>();

        private Func<T, string> m_getLabel;
        private T m_value;
        
        public SearchableDropdown()
        {
            AddToClassList("dk-searchable-dropdown");

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            m_selectedLabel = new Label("Select…")
            {
                pickingMode = PickingMode.Ignore
            };
            m_selectedLabel.AddToClassList("dk-searchable-dropdown__selected");
            m_selectedLabel.RegisterCallback<ClickEvent>(_ => TogglePopup());
            Add(m_selectedLabel);

            // Optional little ▼
            var arrow = new Label("▼");
            arrow.pickingMode = PickingMode.Ignore;
            arrow.AddToClassList("dk-searchable-dropdown__arrow");
            Add(arrow);
            
            RegisterCallback<ClickEvent>(_ => TogglePopup());
        }
        
        public void SetItems(IEnumerable<T> items, Func<T, string> getLabel, Action<T> onSelectionChanged = null)
        {
            if (getLabel == null)
            {
                throw new ArgumentNullException(nameof(getLabel));
            }

            m_getLabel = getLabel;
            SelectionChanged = onSelectionChanged;

            m_allItems.Clear();
            m_allItems.AddRange(items ?? Array.Empty<T>());

            EnsurePopupCreated();
            Filter(string.Empty);
            UpdateLabel();
        }

        public void SetValueWithoutNotify(T newValue)
        {
            SetValueInternal(newValue, false, true);
        }
        
        private void EnsurePopupCreated()
        {
            if (m_overlay != null && m_popup != null)
            {
                return;
            }

            m_overlay = new VisualElement
            {
                name = "SearchableDropdownOverlay"
            };
            m_overlay.AddToClassList("dk-searchable-dropdown__overlay");
            m_overlay.style.position = Position.Absolute;
            m_overlay.style.left = 0;
            m_overlay.style.top = 0;
            m_overlay.style.right = 0;
            m_overlay.style.bottom = 0;

            // Clicking outside closes the popup
            m_overlay.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == m_overlay)
                {
                    ClosePopup();
                }
            });

            m_anchor = new VisualElement();
            m_anchor.style.position = Position.Absolute;
            m_overlay.Add(m_anchor);
            
            m_popup = new VisualElement
            {
                name = "SearchableDropdownPopup"
            };
            
            m_popup.AddToClassList("dk-searchable-dropdown__popup");
            m_anchor.Add(m_popup);

            m_searchField = new TextField
            {
                isDelayed = false,
                value = string.Empty
            };
            m_searchField.AddToClassList("dk-searchable-dropdown__search");
            m_searchField.RegisterValueChangedCallback(OnSearchChanged);

            m_listView = new ListView
            {
                itemsSource = m_filteredItems,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight
            };
            m_listView.AddToClassList("dk-searchable-dropdown__list");
            m_listView.makeItem = MakeItem;
            m_listView.bindItem = BindItem;
            m_listView.itemsChosen += OnItemsChosen;

            m_popup.Add(m_searchField);
            m_popup.Add(m_listView);
        }

        private void TogglePopup()
        {
            if (m_overlay != null && m_overlay.parent != null)
            {
                ClosePopup();
            }
            else
            {
                OpenPopup();
            }
        }

        private void OpenPopup()
        {
            if (panel == null)
            {
                Debug.LogWarning("SearchableDropdown: cannot open popup, element is not attached to a panel yet.");
                return;
            }

            EnsurePopupCreated();

            // Attach overlay to panel root so it sits above everything
            var root = panel.visualTree;
            if (m_overlay.parent == null)
            {
                root.Add(m_overlay);
            }

            // Position popup just under the field
            var world = worldBound; // in panel space for runtime
            m_anchor.style.left = world.x;
            m_anchor.style.top = world.yMax;
            m_anchor.style.minWidth = world.width;
            
            // Reset search
            m_searchField.value = string.Empty;
            Filter(string.Empty);
            
            m_searchField.schedule.Execute(() =>
            {
                m_searchField.Focus();
                m_searchField.SelectAll();
            });
        }

        private void ClosePopup()
        {
            if (m_overlay != null && m_overlay.parent != null)
            {
                m_overlay.RemoveFromHierarchy();
            }
        }
        
        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            Filter(evt.newValue);
        }

        private void Filter(string search)
        {
            search ??= string.Empty;
            var lower = search.ToLowerInvariant();

            m_filteredItems.Clear();

            if (string.IsNullOrEmpty(lower))
            {
                m_filteredItems.AddRange(m_allItems);
            }
            else
            {
                m_filteredItems.AddRange(
                    m_allItems.Where(i => m_getLabel(i).ToLowerInvariant().Contains(lower))
                );
            }

            m_listView.Rebuild();
        }

        private VisualElement MakeItem()
        {
            var label = new Label();
            label.AddToClassList("dk-searchable-dropdown__item");
            return label;
        }

        private void BindItem(VisualElement element, int index)
        {
            var label = (Label)element;

            if (index >= 0 && index < m_filteredItems.Count)
            {
                label.text = m_getLabel(m_filteredItems[index]);
            }
            else
            {
                label.text = string.Empty;
            }
        }
        
        private void OnItemsChosen(IEnumerable<object> chosen)
        {
            var obj = chosen.FirstOrDefault();
            if (obj is not T selectedValue)
            {
                return;
            }
            
            SetValueInternal(selectedValue, true);
            ClosePopup();
        }
        
        private void SetValueInternal(T newValue, bool fromUser, bool skipNotify = false)
        {
            if (EqualityComparer<T>.Default.Equals(m_value, newValue))
            {
                return;
            }

            var previous = m_value;
            m_value = newValue;
            UpdateLabel();

            if (!skipNotify)
            {
                using var e = ChangeEvent<T>.GetPooled(previous, newValue);
                e.target = this;
                SendEvent(e);
            }

            if (fromUser)
            {
                SelectionChanged?.Invoke(newValue);
            }
        }

        private void UpdateLabel()
        {
            if (m_getLabel == null)
            {
                m_selectedLabel.text = m_value != null ? m_value.ToString() : "Select…";
                return;
            }

            m_selectedLabel.text = m_value != null ? m_getLabel(m_value) : "Select…";
        }
    }
}
