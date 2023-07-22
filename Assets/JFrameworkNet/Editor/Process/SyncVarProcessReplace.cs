using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace JFramework.Editor
{
    public static class SyncVarProcessReplace
    {
        public static void Process(ModuleDefinition md)
        {
            foreach (var td in md.Types.Where(td => td.IsClass))
            {
                ProcessClass(td);
            }
        }

        private static void ProcessClass(TypeDefinition td)
        {
            foreach (MethodDefinition md in td.Methods)
            {
                ProcessMethod( md);
            }
            
            foreach (TypeDefinition nested in td.NestedTypes)
            {
                ProcessClass(nested);
            }
        }

        private static void ProcessMethod(MethodDefinition md)
        {
            if (md.Name == ".cctor" || md.Name == CONST.GEN_FUNC || md.Name.StartsWith(CONST.INV_METHOD))
            {
                return;
            }
            
            if (md.IsAbstract)
            {
                return;
            }
            
            if (md.Body is { Instructions: not null })
            {
                for (int i = 0; i < md.Body.Instructions.Count;)
                {
                    Instruction instr = md.Body.Instructions[i];
                    i += ProcessInstruction(md, instr, i);
                }
            }
        }

        private static int ProcessInstruction(MethodDefinition md, Instruction instr, int index)
        {
            if (instr.OpCode == OpCodes.Stfld && instr.Operand is FieldDefinition OpStfLd)
            {
                ProcessSetInstruction(md, instr, OpStfLd);
            }
            
            if (instr.OpCode == OpCodes.Ldfld && instr.Operand is FieldDefinition OpLdfLd)
            {
                ProcessGetInstruction(md, instr, OpLdfLd);
            }
            
            if (instr.OpCode == OpCodes.Ldflda && instr.Operand is FieldDefinition OpLdfLda)
            {
                return ProcessLoadAddressInstruction( md, instr, OpLdfLda, index);
            }
            
            return 1;
        }
        
        private static void ProcessSetInstruction( MethodDefinition md, Instruction i, FieldDefinition opField)
        {
            if (md.Name == ".ctor") return;
            
            if (SyncVarUtils.setter.TryGetValue(opField, out MethodDefinition replacement))
            {
                i.OpCode = OpCodes.Call;
                i.Operand = replacement;
            }
        }
        
        private static void ProcessGetInstruction(MethodDefinition md, Instruction i, FieldDefinition opField)
        {
            if (md.Name == ".ctor") return;
            
            if (SyncVarUtils.getter.TryGetValue(opField, out MethodDefinition replacement))
            {
                i.OpCode = OpCodes.Call;
                i.Operand = replacement;
            }
        }

        private static int ProcessLoadAddressInstruction(MethodDefinition md, Instruction instr, FieldDefinition opField, int index)
        {
            if (md.Name == ".ctor") return 1;
            
            if (SyncVarUtils.setter.TryGetValue(opField, out MethodDefinition replacement))
            {
                Instruction nextInstr = md.Body.Instructions[index + 1];

                if (nextInstr.OpCode == OpCodes.Initobj)
                {
                    ILProcessor worker = md.Body.GetILProcessor();
                    VariableDefinition tmpVariable = new VariableDefinition(opField.FieldType);
                    md.Body.Variables.Add(tmpVariable);

                    worker.InsertBefore(instr, worker.Create(OpCodes.Ldloca, tmpVariable));
                    worker.InsertBefore(instr, worker.Create(OpCodes.Initobj, opField.FieldType));
                    worker.InsertBefore(instr, worker.Create(OpCodes.Ldloc, tmpVariable));
                    worker.InsertBefore(instr, worker.Create(OpCodes.Call, replacement));

                    worker.Remove(instr);
                    worker.Remove(nextInstr);
                    return 4;
                }
            }

            return 1;
        }
    }
}