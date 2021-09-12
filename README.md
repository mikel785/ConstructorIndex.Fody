# <img src="/package_icon.png" height="30px"> ConstructorIndex.Fody

[![NuGet Status](https://img.shields.io/nuget/v/ConstructorIndex.Fody.svg)](https://www.nuget.org/packages/ConstructorIndex.Fody/)

Inject constructor index automatic assignation into constructor code to ensure which one was used to create object.

### This is an add-in for [Fody](https://github.com/Fody/Home/).

## Usage

See also [Fody usage](https://github.com/Fody/Home/blob/master/pages/usage.md).

### NuGet installation

Install the [ConstructorIndex.Fody NuGet package](https://nuget.org/packages/ConstructorIndex.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```powershell
PM> Install-Package Fody
PM> Install-Package ConstructorIndex.Fody
```

The `Install-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


### Add to FodyWeavers.xml

Add `<ConstructorIndex/>` to [FodyWeavers.xml](https://github.com/Fody/Home/blob/master/pages/usage.md#add-fodyweaversxml)

```xml
<Weavers>
  <ConstructorIndex>
    <Full.Name.ClassA/>
    <Full.Name.ClassB/>
    <Full.Name.BaseClass/>
  </ConstructorIndex>
</Weavers>
```

Parameter set:
- NonPublic     [bool]   - tells weaver to process both public and not public contructors.
- PropertyName  [string] - set field name explicitly (?)

```c#
 var usedCtorIndex = obj.GetConstructorIndex();
```

### TODO:
1. Make derived classes processing switchabled. 
2. Maybe we need to set ClassName with regex?
3. Will it be good to store constuctor signature instead of index?

### DONE:
1. Inject special internal or private attribute to mark processed class with it.
   Also it will keep field name where stored ctor index.


0.0.5:
- Code refactoring.
- Removed ClassName [string] - full type name of target class, derived class will be processed too.
  Set target class names inside ConstructorIndex node:
  <ConstructorIndex>
    <Full.Name.ClassA/>
    <Full.Name.ClassB/>
  </ConstructorIndex>

Mikhail Kanygin
