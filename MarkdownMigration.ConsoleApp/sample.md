@System.String and @outlook.com

Test markdown within html:

<div>
**markdown**

</div>

```html
	<div>
	**markdown**
	</div>
```

Test blockquote:
>
> This is blockquote
> and another


In general, symbol bindings go out of scope and become inoperative 
at the end of the statement block they occur in. 
There are two exceptions to this rule: 
 - The binding of the loop variable of a `for` loop is in scope for 
    the body of the for loop, but not after the end of the loop. 
 - All three portions of a `repeat`/`until` loop (the body, the test, 
    and the fixup) are treated as a single scope, so symbols that are 
    bound in the body are available in the test and in the fixup. 
For both types of loops, each pass through the loop executes in its own scope, 
so bindings from an earlier pass are not available in a later pass.


...