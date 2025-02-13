using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Talent.GraphEditor.Unity.Runtime
{
    /// <summary>
    /// Компонент, представляющий элемент интерфейса. Данный класс может либо отображать текст, либо несколько изображений
    /// </summary>
    public class Icon : MonoBehaviour
    {
        /// <summary>
        /// Первое изображение
        /// </summary>
        public Image firstImage;
        /// <summary>
        /// Второе изображение
        /// </summary>
        public Image secondImage;
        /// <summary>
        /// Изображение, разделяющее первое и второе изображения
        /// </summary>
        public Image separator;

        [SerializeField] private TextMeshProUGUI _text;

        /// <summary>
        /// Задает определенный текст и отключает все изображения
        /// </summary>
        /// <param name="text">Текст, который будет задан</param>
        public void SetText(string text)
        {
            if (_text != null)
            {
                _text.text = text;
                _text.gameObject.SetActive(true);
            }

            if (firstImage != null)
            {
                firstImage.gameObject.SetActive(false);
            }

            if (secondImage != null)
            {
                secondImage.gameObject.SetActive(false);
            }

            if (separator != null)
            {
                separator.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Обновляет изображения и отключает текст
        /// </summary>
        /// <param name="iconProvider">Класс, предоставляющий доступ к спрайтам</param>
        /// <param name="currentTrigger">Уникальный идентификатор иконки</param>
        public void UpdateIcons(IconSpriteProviderAsset iconProvider, string currentTrigger)
        {
            var icons = iconProvider.GetIcons(currentTrigger);

            if (_text != null)
            {
                _text.gameObject.SetActive(false);
            }

            switch (icons.Count)
            {
                case 1:
                    firstImage.gameObject.SetActive(true);
                    firstImage.sprite = icons[0];

                    secondImage.gameObject.SetActive(false);

                    separator.gameObject.SetActive(false);
                    break;
                case 2:
                    firstImage.gameObject.SetActive(true);
                    firstImage.sprite = icons[0];

                    secondImage.gameObject.SetActive(true);
                    secondImage.sprite = icons[1];

                    separator.gameObject.SetActive(true);

                    break;
            }
        }
    }
}
