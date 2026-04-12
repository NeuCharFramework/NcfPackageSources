[中文版](readme.cn.md)

﻿## Debugging instructions

The independent library of Razor Page needs to be compiled into a dll and then referenced by the Senparc.Web project to take effect.

Therefore, if you need to quickly debug the page content in .cshtml (to avoid debugging every time), you can move this folder (Areas) as a whole to the root directory of Senparc.Web.

## Security

In order to improve security, we recommend restricting access to the Admin module in the online production environment (such as restricting IP, restricting local access, or loading it elsewhere instead of integrating it in the production environment).

At the same time, you can also improve the security of the background by randomly modifying the Area name.
