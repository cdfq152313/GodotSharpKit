extends RefCounted

var X: int = 0
var getter_only: int = 300
var setter_only: String

signal MySignal()
signal my_signal_param(a: int, b:Node)
signal my_signal_param_with_generic(array)


func PrintSetterOnly():
	print("SetterOnly: ", setter_only)

func my_method(a: int, b: Node):
	print("MyMethod: ", a, "/", b)

func my_method_and_return_value(a: int, b: Node):
	print("MyMethodAndReturnValue: ", a, "/",b)
	return "abc"
