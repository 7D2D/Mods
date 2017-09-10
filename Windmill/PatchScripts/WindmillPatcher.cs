using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SDX.Compiler;

public class WindmillPatcher : IPatcherMod
{
    
    public bool Patch(ModuleDefinition module)
    {
        var console = module.Types.First(d => d.Name == "TileEntityPowered");
        foreach (var field in console.Fields)
            SetFieldToPublic(field);

        AddEnumOption(module, "PowerItemTypes", "Windmill", 12);

        return true;
    }

    private void InjectCreateItem(ModuleDefinition vanilla, ModuleDefinition mod)
    {
        var constructor = vanilla.Import(mod.Types.First(d=> d.Name == "WindmillPowerItem").Methods.First(d => d.Name == ".ctor"));
        var powerItem = vanilla.Types.First(d => d.Name == "PowerItem");

        var create = powerItem.Methods.First(d => d.Name == "CreateItem");
        
        var pro = create.Body.GetILProcessor();

        var instructions = pro.Body.Instructions;

        Instruction switchInstruction = null;
        foreach (var i in instructions)
        {
            if (i.OpCode == OpCodes.Switch)
            {
                switchInstruction = i;
                break;
            }
        }

        var lastRet = instructions.Last(d => d.OpCode == OpCodes.Ret);

        pro.InsertAfter(lastRet, Instruction.Create(OpCodes.Ret));
        var jumpTo = Instruction.Create(OpCodes.Newobj, constructor);
        pro.InsertAfter(lastRet, jumpTo);

        var list = ((Instruction[]) switchInstruction.Operand).ToList();
        list.Add(jumpTo);
        switchInstruction.Operand = list.ToArray();

    }

    private void AddEnumOption(ModuleDefinition gameModule, string enumName, string enumFieldName, byte enumValue)
    {
        var enumType = gameModule.Types.First(d=> d.Name == "PowerItem").NestedTypes.First(d => d.Name == enumName);
        FieldDefinition literal = new FieldDefinition(enumFieldName, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, enumType);
        enumType.Fields.Add(literal);
        literal.Constant = enumValue;
    }

    public bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {
        InjectCreateItem(gameModule, modModule);
        return true;
    }
    
    private void SetMethodToVirtual(MethodDefinition meth)
    {
        meth.IsVirtual = true;
    }
    private void SetFieldToPublic(FieldDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }

    private void SetClassToPublic(TypeDefinition classDef)
    {

        if (classDef == null) return;

        classDef.IsPublic = true;
        classDef.IsNotPublic = false;

    }
    private void SetNestedClassToPublic(TypeDefinition classDef)
    {
        if (classDef == null) return;
        classDef.IsNestedPublic = true;
    }

    private void SetMethodToPublic(MethodDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
}
