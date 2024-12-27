extends Node2D

func _ready() -> void:
	set_process(false)

	if not Composer.has_initialized:
		await Composer.finished_initialising
	print("load")
	Composer.finished_loading.connect(on_finished_loading)
	Composer.load("res://Menu/menu.tscn")

func on_finished_loading(scene: Node) -> void:
	Composer.loading_activated.connect(scene.on_loading_activated)
	$CanvasLayer/LoadScreen/FinishedLabel.show()
	$CanvasLayer/LoadScreen/FinishedLabel/AnimationPlayer.play("FadeInOut")
	set_process(true)

func fade_out() -> void:
	var fade_tween: Tween = get_tree().create_tween()
	fade_tween.tween_property($CanvasLayer/LoadScreen/FadeRect,"color:a",0,0.75)
	fade_tween.tween_callback(func() -> void:
		fade_tween.kill()
		Composer.clear_load_screen()
	)

func _process(delta: float) -> void:
	if Input.is_action_just_pressed("activate"):
		$CanvasLayer/LoadScreen/FinishedLabel.hide()
		Composer.loading_activated.emit()
		fade_out()
		set_process(false)
