# Hosihikari.Bindings.LeviLamina

[English](README.md) | [简体中文](README.zh_hans.md)

星光绑定中的 LeviLamina LLAPI 面：CI 生成的原生风格 **Interop**、手写 .NET **Idiomatic** 层，以及原生 **BindingShim**。

## 边界

- `Hosihikari.Bindings.LeviLamina.Interop` 由 `Hosihikari/Bindings.Generator` 从版本化 IR 生成。它镜像 LLAPI 声明，保留 `nint`/不透明句柄；提交入库并由 CI 重跑比对。
- `Hosihikari.Bindings.LeviLamina` 是手写 .NET 层：属性、事件、运算符、索引器、构造函数与服务端线程调度器。它不进入生成流水线。
- `native/BindingShim/` 是用 LeviLamina 的 xmake/LLVM 工具链编译的 C ABI 投影。它不托管 CLR；HostingShim 是独立的一层。

## 当前状态

Interop 与 Idiomatic 工程可构建。原生 BindingShim 目标尚未实现。

## 构建

```shell
dotnet build Hosihikari.Bindings.LeviLamina.slnx -c Release
```

包版本跟随上游版本 + 绑定修订（`26.51.7-r0`）。该面的平台是 Windows x64。

## 相关仓库

- `Hosihikari/Bindings.Ir`：schema、模型、读写器、校验器。
- `Hosihikari/Bindings.Generator`：libTooling 前端与发射器。

## 许可证

LGPL-3.0-or-later，见 `LICENSE.md`。
