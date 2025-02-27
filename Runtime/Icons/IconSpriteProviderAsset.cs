using System;
using System.Collections.Generic;
using UnityEngine;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Ассет, реализующий интерфейс <see cref="IIconProvider{TIconType}"/>
    /// </summary>
    [CreateAssetMenu(fileName = "Icon Sprite Provider Asset", menuName = "Graph Editor/Icon Sprite Provider Asset", order = 1)]
    public class IconSpriteProviderAsset : ScriptableObject, IIconProvider<Sprite>
    {
        [SerializeField] private Icon _singleIconPrefab;

        /// <summary>
        /// Возвращает префаб иконки, состоящий из одного изображения
        /// </summary>
        public Icon SingleIconPrefab => _singleIconPrefab;
        [SerializeField] private Icon _doubleIconPrefab;
        /// <summary>
        /// Возвращает префаб иконки, состоящий из двух изображений
        /// </summary>
        public Icon DoubleIconPrefab => _doubleIconPrefab;
        [SerializeField] private Sprite _defaultIcon;
        /// <summary>
        /// Возвращает стандартный спрайт иконки
        /// </summary>
        public Sprite DefaultIcon => _defaultIcon;

        [SerializeField] private List<IconSprite> _icons = new();
        /// <summary>
        /// Возвращает иконки, состоящие из спрайта, цвета обводки и уникального идентификатора
        /// </summary>
        public IEnumerable<IconSprite> Icons => _icons;

        private Dictionary<string, Sprite> _iconsDictionary;
        private Dictionary<string, Color> _colorsDictionary;
    
        /// <summary>
        /// Копирует содержимое данного ассета в другой ассет
        /// </summary>
        /// <param name="iconSpriteProviderAsset">Ассет, в который происходит копирование</param>
        public void CopyAsset(IconSpriteProviderAsset iconSpriteProviderAsset)
        {
            _singleIconPrefab = iconSpriteProviderAsset.SingleIconPrefab;
            _doubleIconPrefab = iconSpriteProviderAsset.DoubleIconPrefab;
            _defaultIcon = iconSpriteProviderAsset.DefaultIcon;

            AddIcons(iconSpriteProviderAsset.Icons);
        }

        /// <summary>
        /// Добавляет иконки в ассет
        /// </summary>
        /// <param name="icons"></param>
        public void AddIcons(IEnumerable<IconSprite> icons)
        {
            _icons.AddRange(icons);
        }

        /// <inheritdoc/>
        public bool TryGetIcon(string key, out Sprite icon)
        {
            CheckDictionary();

            if (_iconsDictionary.ContainsKey(key))
            {
                icon = _iconsDictionary[key];
                return true;
            }
            else
            {
                icon = _defaultIcon;
                return false;
            }
        }

        /// <summary>
        /// Возвращает спрайты по уникальному идентификатору
        /// </summary>
        /// <param name="key">Уникальный идентификатор</param>
        /// <returns>Список спрайтов, соответствующий переданному идентификатору</returns>
        public List<Sprite> GetIcons(string key)
        {
            string[] iconKeys = key.Split('.', System.StringSplitOptions.RemoveEmptyEntries);
            List<Sprite> icons = new List<Sprite>();

            CheckDictionary();

            switch (iconKeys.Length)
            {
                case 1:
                    TryGetIcon(iconKeys[0], out Sprite iconSprite);

                    icons.Add(iconSprite);
                    break;
                case 2:
                    TryGetIcon(iconKeys[0], out Sprite moduleIconSprite);

                    icons.Add(moduleIconSprite);

                    if (!TryGetIcon(key, out iconSprite))
                    {
                        TryGetIcon(iconKeys[1], out iconSprite);
                    }

                    icons.Add(iconSprite);
                    break;
            }

            return icons;
        }

        /// <summary>
        /// Пытается получить цвет обводки иконки по уникальному идентификатору
        /// </summary>
        /// <param name="key">Уникальный идентификатор иконки</param>
        /// <param name="color">Возвращает цвет обводки, если иконка с таким идентификатором найдена, иначе белый</param>
        /// <returns></returns>
        public bool TryGetColor(string key, out Color color)
        {
            string[] iconKeys = key.Split('.', System.StringSplitOptions.RemoveEmptyEntries);

            if (iconKeys.Length == 0)
            {
                color = Color.white;
                return false;
            }

            key = iconKeys[0];

            if (_iconsDictionary.ContainsKey(key))
            {
                color = _colorsDictionary[key];
                return true;
            }
            else
            {
                color = Color.white;
                return false;
            }
        }

        /// <summary>
        /// Получает экземпляр иконки по переданному уникальному идентификатору
        /// </summary>
        /// <param name="key">Уникальный идентификатор иконки</param>
        /// <param name="singleIconPrefab">Иконка, состоящая из одного изображения</param>
        /// <param name="doubleIconPrefab">Иконка, состоящая из двух изображений</param>
        /// <param name="changeSeparatorColor">Нужно, ли изменять цвет разделителя изображений</param>
        /// <returns>Возвращает игровой объект иконки</returns>
        public GameObject GetIconInstance(string key, Icon singleIconPrefab = null, Icon doubleIconPrefab = null, bool changeSeparatorColor = false)
        {
            Icon currentIcon = null;

            CheckDictionary();

            string[] iconKeys = key.Split('.', System.StringSplitOptions.RemoveEmptyEntries);

            if (iconKeys.Length == 1 && int.TryParse(iconKeys[0], out _) || float.TryParse(iconKeys[0], out _))
            {
                currentIcon = GameObject.Instantiate(singleIconPrefab == null ? _singleIconPrefab : singleIconPrefab);
                currentIcon.SetText(iconKeys[0]);

                return currentIcon.gameObject;
            }

            switch (iconKeys.Length)
            {
                case 1:
                    currentIcon = GameObject.Instantiate(singleIconPrefab == null ? _singleIconPrefab : singleIconPrefab);

                    TryGetIcon(iconKeys[0], out Sprite iconSprite);

                    currentIcon.firstImage.sprite = iconSprite;
                    break;
                case 2:
                    currentIcon = GameObject.Instantiate(doubleIconPrefab == null ? _doubleIconPrefab : doubleIconPrefab);

                    TryGetIcon(iconKeys[0], out Sprite moduleIconSprite);

                    currentIcon.firstImage.sprite = moduleIconSprite;

                    if (!TryGetIcon(key, out iconSprite))
                    {
                        TryGetIcon(iconKeys[1], out iconSprite);
                    }

                    if (changeSeparatorColor && TryGetColor(key, out Color color) && currentIcon.separator != null)
                    {
                        currentIcon.separator.color = color;
                    }

                    currentIcon.secondImage.sprite = iconSprite;
                    break;
            }

            return currentIcon.gameObject;
        }

        private void CheckDictionary()
        {
            if (_iconsDictionary != null && _colorsDictionary != null)
            {
                return;
            }

            _iconsDictionary = new Dictionary<string, Sprite>();

            for (int i = 0; i != _icons.Count; i++)
            {
                if (_iconsDictionary.ContainsKey(_icons[i].key))
                {
                    Debug.LogWarning($"[IconSpriteProviderAsset] Dictionary already has '{_icons[i].key}' key");
                }
                else
                {
                    _iconsDictionary.Add(_icons[i].key, _icons[i].icon);
                }
            }

            _colorsDictionary = new Dictionary<string, Color>();

            for (int i = 0; i != _icons.Count; i++)
            {
                if (_colorsDictionary.ContainsKey(_icons[i].key))
                {
                    Debug.LogWarning($"[IconSpriteProviderAsset] Dictionary already has '{_icons[i].key}' key");
                }
                else
                {
                    _colorsDictionary.Add(_icons[i].key, _icons[i].OutlineColor);
                }
            }
        }

        /// <summary>
        /// Иконка, содержащая в себе уникальный идентификатор, спрайт и цвет обводки
        /// </summary>
        [Serializable]
        public class IconSprite
        {
            /// <summary>
            /// Уникальный идентификатор, использующийся для поиска
            /// </summary>
            public string key;
            /// <summary>
            /// Спрайт иконки
            /// </summary>
            public Sprite icon;
            /// <summary>
            /// Цвет обводки иконки
            /// </summary>
            public Color OutlineColor = new Color(250, 205, 88);

            /// <summary>
            /// Конструктор <see cref="IconSprite"/>
            /// </summary>
            /// <param name="key">Уникальный идентификатор</param>
            /// <param name="icon">Спрайт иконки</param>
            /// <param name="outlineColor">Цвет обводки иконки</param>
            public IconSprite(string key, Sprite icon, Color outlineColor)
            {
                this.key = key;
                this.icon = icon;
                this.OutlineColor = outlineColor;
            }
        }
    }
}
