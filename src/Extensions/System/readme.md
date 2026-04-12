[中文版](readme.cn.md)

# System folder description

This folder contains system-specified XNCF top-level modules.

## Notice

`Senparc.Xncf.SystemCore` is a prerequisite module required for all modules to start, so every project must be added (especially database projects).

> We will consider integrating it into a dedicated DatabaseCore project later.

## Numeric prefix

The number in the naming prefix (such as `[5999]`) indicates the execution priority of the module during the startup process. The larger the number, the higher the execution priority.
