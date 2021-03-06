﻿using ProjectHekate.Scripting.Interfaces;

namespace ProjectHekate.Scripting.Bytecode.Emitters
{
    public class IfStatementEmitter : EmptyEmitter
    {
        private readonly IBytecodeEmitter _ifBodyStatement;
        private readonly IBytecodeEmitter _elseBodyStatement;
        private readonly IBytecodeGenerator _ifConditionExpression;

        public IfStatementEmitter(IBytecodeEmitter ifBodyStatement, IBytecodeEmitter elseBodyStatement, IBytecodeGenerator ifConditionExpression)
        {
            _ifBodyStatement = ifBodyStatement;
            _elseBodyStatement = elseBodyStatement;
            _ifConditionExpression = ifConditionExpression;
        }
        
        public override void EmitTo(ICodeBlock codeBlock, IVirtualMachine vm, IScopeManager scopeManager)
        {
            // If statement code
            // Generate ifConditionExpression code
            // Instruction.JumpIfZero
            // The location to jump if ifConditionExpression evaluates to 0
            // Generate expression code for statement
            // Instruction.Jump if needed
            // location to jump if needed
            // Generate code for statement (if it exists)

            codeBlock.Add(_ifConditionExpression.Generate(vm, scopeManager));
            codeBlock.Add(Instruction.IfZeroBranch);

            var conditionalIdx = codeBlock.Size;
            codeBlock.Add(0.0f); // dummy value, will be changed later

            _ifBodyStatement.EmitTo(codeBlock, vm, scopeManager);

            codeBlock[conditionalIdx] = codeBlock.Size; // now itll jump to the else, if it exists

            if (_elseBodyStatement != null) {
                codeBlock[conditionalIdx] += 2; // update the previous jump
                codeBlock.Add(Instruction.Jump); // jump past the else

                var elseIdx = codeBlock.Size;
                codeBlock.Add(0.0f); // dummy value, will be changed later

                _elseBodyStatement.EmitTo(codeBlock, vm, scopeManager);

                codeBlock[elseIdx] = codeBlock.Size;
            }
        }
    }
}
