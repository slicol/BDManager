# BDManager

有一些文件上传到百度云（或者其它云盘），会因为政策原因而被云盘屏蔽。目前屏蔽的依据有两种：其一是根据文件名，其二是根据文件Hash值。因此，我提供一个小工具，可以对文件进行简单的打包，将文件名等信息与文件内容打包成自定义格式的文件，并且以1.bds,2.bds,3.bds...这样的方式命名，从而避免屏蔽。

该工具命令格式：
BDTools.exe [-pack | -unpack ] -dir -all [-src [srcpath|srcdir] -dst dstdir] [-keep]

例如：
BDTools.exe -pack -src xxx.mp4
将xxx.mp4在同级目录下打包生成.bds文件

BDTools.exe -pack -dir -all
将当前BDTools.exe所在目录下的文件，递归地打包生成.bds文件

BDTools.exe -unpack -dir -all
将当前BDTools.exe所有目录下的bds文件，递归地还原成原文件

