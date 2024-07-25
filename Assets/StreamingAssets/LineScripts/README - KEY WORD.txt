/// <summary>
/// <list type="bullet"><b>using</b> class-name</list>
/// In this script, this class will support functions and fields, which is needed
/// </summary>
using
/// <summary>
/// <list type="bullet"><b>import</b> script</list>
/// Reference another script, and the code for that script will be sub script
/// </summary>
import
/// <summary>
/// <list type="bullet"><b>include</b> script</list>
/// Reference another script, and the code for that script will replace this line
/// </summary>
include
/// <summary>
/// <list type="bullet"><b>if</b> literal-value/symbol-word</list>
/// If the literal-value or symbol-word is equal to 0, the result is false
/// </summary>
if
/// <summary>
/// <list type="bullet"><b>else</b> <see langword="if"/></list>
/// Else is always equivalent to a correct if, However, if the previous statement is executed from within the if block, the block will be ignored
/// </summary>
else
/// <summary>
/// <list type="bullet"><b>while</b> literal-value/symbol-word</list>
/// Exits only if the literal-value or symbol-word is equal to 0
/// </summary>
while
/// <summary>
/// <list type="bullet"><b>break</b></list>
/// Exit the current block immediately
/// </summary>
break
/// <summary>
/// <list type="bullet"><b>continue</b></list>
/// Immediately move to the tail of the current block
/// </summary>
continue
/// <summary>
/// <list type="bullet"><b>define</b> <see langword="symbol"/>(will be defined) literal-value/symbol-word</list>
/// <list type="bullet"><b>define</b>(<see langword="symbol"/>) literal-value/symbol-word</list>
/// Define a reference for aim word
/// <list type="bullet"><b>define</b> <see langword="symbol"/> = literal-value</list>
/// Define a expression on <see cref="Diagram.Arithmetic.ArithmeticExtension"/>
/// </summary>
define
/// <summary>
/// <b>Target class-type is recommended to have only one constructor</b>
/// <list type="bullet"><b>new</b>(<see langword="symbol"/>) class-name([literal-value/symbol-word])</list>
/// Generate a new instance of target type and named <see langword="symbol"/>, arguments is optional
/// <list type="bullet"><b>new</b>(<see langword="_"/>) class-name([literal-value/symbol-word])</list>
/// Generate a new anonymity instance of target type, arguments is optional
/// </summary>
new
/// <summary>
/// <list type="bullet"><b>delete</b> symbol-word</list>
/// Try to remove one reference of target core
/// </summary>
delete
/// <summary>
/// <list type="bullet"><b>call</b> script <see langword="import"/> (arg-name=arg-value)...</list>
/// Reference other script and pass in parameters, it will run in a new core
/// </summary>
call
