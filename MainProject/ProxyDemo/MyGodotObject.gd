extends RefCounted

var x: int = 0
var GetterOnly: int = 300
var SetterOnly: String

signal my_signal()
signal MySignalParam(a: int, b:Node)
signal MySignalParamWithGeneric(array)


func print_setter_only():
	print("SetterOnly: ", SetterOnly)

func MyMethod(a: int, b: Node):
	print("MyMethodWithParam: ", a, "/", b)

func MyMethodAndReturnValue(a: int, b: Node):
	print("MyMethodWithParamAndReturnValue: ", a, "/",b)
	return "abc"

func EmitAll():
	my_signal.emit()
	MySignalParam.emit(1, Node.new())
	MySignalParamWithGeneric.emit([1,2,3,4])
