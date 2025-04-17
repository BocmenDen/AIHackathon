using AIHackathon.Base;
using AIHackathon.Extensions;
using BotCore.Tg;
using System.Text.RegularExpressions;

namespace AIHackathon.Pages
{
    [PageCacheable(Key)]
    public partial class PageInfo : PageBase
    {
        public const string Key = "PageInfo";
        private const string RootChapters = "1 Общая информация";
        private const string DatasetInfoChapters = "2 Подробнее о датасете";
        private const string DemoInfoChapters = "3 Пример фотографии";
        private const string LibraryInfoChapters = "4 О доступных библиотеках";
        private const string SendModelInfoChapters = "5 Отправка модели";
        private const string EvaluationRulesInfoChapters = "6 Метрика и правила";
        private readonly static ButtonsSend buttonsChapter = new([[RootChapters], [DatasetInfoChapters], [DemoInfoChapters], [LibraryInfoChapters], [SendModelInfoChapters], [EvaluationRulesInfoChapters]]);
        private readonly static Dictionary<string, string> _infos = new()
        {
            { RootChapters,
@"
*О боте и хакатоне*:  
Бот используется для оценки обученных моделей. Участники могут загружать свои модели, видеть точность их работы и сравнивать результаты с другими командами.

*Задача*:  
Необходимо разработать модель для предсказания координат ключевых точек лица (глаз, нос, рот и т.п.) на изображениях размером 92x92 пикселя в чёрно-белом формате. Яркость пикселей должна быть нормализована в диапазон от 0 до 1. Отсчёт координат ведётся от верхнего левого угла изображения, ось X направлена вправо, ось Y — вниз.

*Коротко о разделах*:
- *Рейтинг*: Страница с рейтингом команд. Можно перемещаться по списку, по умолчанию отображается команда участника с символом 🎯.
- *Главная*: Страница с информацией о команде участника, включая общий рейтинг и лучшие результаты.
- *Новости*: Раздел с ссылкой на группу новостей.
" },
            { DatasetInfoChapters,
@"
*Подробнее о датасете*:  
Для выполнения задачи используется датасет с изображениями размером 92x92 пикселя в чёрно-белом формате. Важные моменты:

*Размер изображений*: все изображения имеют размер 92x92 пикселя.
*Формат изображений*: чёрно-белые.
*Нормализация*: яркость каждого пикселя нормализована в диапазон от 0 до 1.
*Аргументы при обучении*:

1. *`x_train`*: массив входных изображений. Форма данных может зависеть от используемой библиотеки:
1.1 Для *Keras*: форма данных будет `[N, 92, 92, 1]`, где `N` — количество изображений.
1.2 Для *Scikit-learn* или *XGBoost*: форма может быть `[N, 92*92]`, где каждое изображение превращается в одномерный вектор длиной 8464 (92 x 92).
			  
2. *`y_train`*: Массив с координатами ключевых точек лица. Значения координат указаны в пикселях, отсчёт ведётся от верхнего левого угла изображения. Для каждого изображения предсказывается 30 значений, по две координаты (X и Y) для каждой из 15 ключевых точек. Формат массива: `[N, 30]`, где `N` — количество изображений.

Список ключевых точек, которые необходимо предсказать:
*Глаза*: 
	- Центр левого глаза: `left_eye_center_x`, `left_eye_center_y`
	- Центр правого глаза: `right_eye_center_x`, `right_eye_center_y`
	- Внутренний угол левого глаза: `left_eye_inner_corner_x`, `left_eye_inner_corner_y`
	- Наружный угол левого глаза: `left_eye_outer_corner_x`, `left_eye_outer_corner_y`
	- Внутренний угол правого глаза: `right_eye_inner_corner_x`, `right_eye_inner_corner_y`
	- Наружный угол правого глаза: `right_eye_outer_corner_x`, `right_eye_outer_corner_y`
			  
*Брови*:
	- Внутренний конец левой брови: `left_eyebrow_inner_end_x`, `left_eyebrow_inner_end_y`
	- Наружный конец левой брови: `left_eyebrow_outer_end_x`, `left_eyebrow_outer_end_y`
	- Внутренний конец правой брови: `right_eyebrow_inner_end_x`, `right_eyebrow_inner_end_y`
	- Наружный конец правой брови: `right_eyebrow_outer_end_x`, `right_eyebrow_outer_end_y`
			  
*Нос*:
	- Кончик носа: `nose_tip_x`, `nose_tip_y`
			  
*Рот*:
	- Левый угол рта: `mouth_left_corner_x`, `mouth_left_corner_y`
	- Правый угол рта: `mouth_right_corner_x`, `mouth_right_corner_y`
	- Центральная точка верхней губы: `mouth_center_top_lip_x`, `mouth_center_top_lip_y`
	- Центральная точка нижней губы: `mouth_center_bottom_lip_x`, `mouth_center_bottom_lip_y`

Порядок входных точек для предсказаний:
```left_eye_center_x,left_eye_center_y,right_eye_center_x,right_eye_center_y,left_eye_inner_corner_x,left_eye_inner_corner_y,left_eye_outer_corner_x,left_eye_outer_corner_y,right_eye_inner_corner_x,right_eye_inner_corner_y,right_eye_outer_corner_x,right_eye_outer_corner_y,left_eyebrow_inner_end_x,left_eyebrow_inner_end_y,left_eyebrow_outer_end_x,left_eyebrow_outer_end_y,right_eyebrow_inner_end_x,right_eyebrow_inner_end_y,right_eyebrow_outer_end_x,right_eyebrow_outer_end_y,nose_tip_x,nose_tip_y,mouth_left_corner_x,mouth_left_corner_y,mouth_right_corner_x,mouth_right_corner_y,mouth_center_top_lip_x,mouth_center_top_lip_y,mouth_center_bottom_lip_x,mouth_center_bottom_lip_y```
" },
            { LibraryInfoChapters,
@"
📦 *Поддерживаемые форматы:*

Модель может быть загружена в одном из следующих форматов:
- `.h5` — для моделей *Keras*
- `.pkl` — для моделей *Scikit-learn*
- `.model` — для моделей *XGBoost*
- `.ubj` — для моделей *XGBoost*
- `.keras` — для моделей *Keras*

⚠️ *Важно:*  
Для корректной загрузки и работы моделей *обязательно используйте те же версии библиотек*, которые применялись при тестировании:

```
numpy=2.0.2  
pandas=2.2.3  
tensorflow=2.18.0  
scikit-learn=1.5.2  
xgboost=2.1.2  
```

Несовпадение версий может привести к ошибкам при загрузке или некорректному тестированию моделей.
" },
            { SendModelInfoChapters,
$@"
📤 *Отправка модели*

Хочешь проверить свою модель? Вот как это сделать:

🛠 Ручной способ:
*Сохрани модель* в одном из поддерживаемых форматов: `h5`, `pkl`, `model`, `xgb`, `ubj`, `keras`
*Разбей модель на части* с помощью скрипта: /{ConstsShared.ScriptCommandDefault}
*Отправь все части модели боту*

🤖 Автоматическое сохранение:
Хотите, чтобы сохранение и разбиение на файлы делалось одной командой? Легко!
- *Универсальный способ* — подходит для всех поддерживаемых библиотек: /{ConstsShared.ScriptCommandAuto}
- *Для Keras* /{ConstsShared.ScriptCommandKeras}
- *Для Sklearn* /{ConstsShared.ScriptCommandSklearn}
- *Для XGBoost* /{ConstsShared.ScriptCommandXGBoost}

📌 *Важно:*
- Неважно, в каком порядке ты отправляешь файлы — главное, чтобы *все части* модели были получены.
- Бот сам соберёт модель и начнёт её проверку 🧪

💡 *Совет:*  
Убедись, что используешь *те же версии библиотек*, что и в тестировании:

```
numpy==2.0.2  
pandas==2.2.3  
tensorflow==2.18.0  
scikit-learn==1.5.2  
xgboost==2.1.2  
```

Несовпадение версий может привести к ошибкам ⚠️
" },
            { EvaluationRulesInfoChapters,
$@"
*Метрика и ограничения*  
Каждая модель проверяется на заранее подготовленном тестовом наборе данных. Для оценки точности используется комбинированная метрика:

```
MSE / 100 + MAE
```

- *MSE (Mean Squared Error)* — среднеквадратичная ошибка.  
- *MAE (Mean Absolute Error)* — средняя абсолютная ошибка.  

💡 *Чем меньше значение метрики — тем выше точность модели.*  
Низкая метрика означает, что модель точно предсказывает координаты ключевых точек на изображениях.

*Ограничения при отправке*  
- 📤 *Одно сообщение за раз:* пока бот обрабатывает какое-либо сообщение, все новые игнорируются.

- 🛡 *Фильтрация спама:* если пользователь отправил более *5 сообщений за 3 секунды*, бот *игнорирует* сообщения в течении *5 минут*.

- 🎯 *Лимит на тестирования:* каждой команде даётся *100 попыток* на проверку обученных моделей за время хакатона. После исчерпания лимита команда не сможет отправлять новые модели на оценку.
"  },
            { DemoInfoChapters,
$@"
Каждое изображение — это лицо в градациях серого размером *96×96 пикселей*.
" },
        };
        private readonly static Dictionary<string, List<MediaSource>> _mediasInfos = new()
        {
            { DemoInfoChapters, [MediaSource.FromFile("./Resources/DemoStart.jpg"), MediaSource.FromFile("./Resources/Demo.jpg")] }
        };

        protected string _chapter = RootChapters;

        public override Task HandleNewUpdateContext(UpdateContext context)
        {
            var buttons = context.BotFunctions.GetIndexButton(context.Update, buttonsChapter);
            if (buttons is ButtonSearch btnS)
            {
                if (_chapter == btnS.Button.Text) return Task.CompletedTask;
                _chapter = btnS.Button.Text;
            }
            if (!_infos.TryGetValue(_chapter, out var text)) return context.ReplyBug("Не найден раздел для справки");
            _mediasInfos.TryGetValue(_chapter, out var medias);
            return context.Reply(new SendModel()
            {
                Message = ToMarkdownV2Escaped(text),
                Inline = new ButtonsSend(buttonsChapter.Buttons.Select(x =>
                {
                    if (x[0].Text == _chapter)
                        return [new ButtonSend($"📌 {x[0].Text}")];
                    return x;
                })),
                Medias = medias
            }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.MarkdownV2));
        }

        public static string ToMarkdownV2Escaped(string input) => RegexEscape().Replace(input, @"\$1");
        [GeneratedRegex(@"([\\.\-()#=!\[\]])")]
        private static partial Regex RegexEscape();
    }
}
