*Подробнее о датасете*:  
Для выполнения задачи используется датасет с изображениями размером 96x96 пикселя в чёрно-белом формате. Важные моменты:

*Размер изображений*: все изображения имеют размер 96x96 пикселя.
*Формат изображений*: чёрно-белые.
*Нормализация*: яркость каждого пикселя нормализована в диапазон от 0 до 1.
*Аргументы при обучении*: Формат входных данных `[N, 96, 96, 1]`, где `N` — количество изображений.

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