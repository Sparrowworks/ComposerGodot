class_name LoadScreen extends Node2D

var _scene: Node

func _ready() -> void:
	Composer.updated_loading.connect(update_load_screen)
	Composer.finished_loading.connect(loading_finished)
	set_process(false)

func update_load_screen(progress: int) -> void:
	$CanvasLayer/LoadScreen/ProgressBar.value = progress
	print("Progress: ", str(progress))

func loading_finished(scene: Node) -> void:
	_scene = scene
	Composer.loading_activated.connect(_scene.on_loading_activated)

	$CanvasLayer/LoadScreen/ProgressBar.value = 100
	$CanvasLayer/LoadScreen/FinishedLabel.show()
	$CanvasLayer/LoadScreen/FinishedLabel/AnimationPlayer.play("FadeInOut")
	set_process(true)
	print("Finished")

func _process(delta: float) -> void:
	if Input.is_action_just_pressed("activate"):
		set_process(false)
		$CanvasLayer/LoadScreen/FinishedLabel.hide()
		Composer.loading_activated.emit()
		Composer.loading_activated.disconnect(_scene.on_loading_activated)
		Composer.clear_load_screen()
