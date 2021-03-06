﻿using System;
using ProjectHekate.Scripting.Helpers;
using ProjectHekate.Scripting.Interfaces;

namespace ProjectHekate.Scripting.Bytecode.Emitters
{
    public class VariableDeclarationStatementEmitter : EmptyEmitter
    {        
        private readonly IBytecodeGenerator _valueExpression;
        private readonly string _variableName;

        public VariableDeclarationStatementEmitter(IBytecodeGenerator valueExpression, string variableName)
        {
            _valueExpression = valueExpression;
            _variableName = variableName;
        }

        public override ICodeBlock Generate(IVirtualMachine vm, IScopeManager scopeManager)
        {
            // NOTE: this declaration only happens for numeral assignments
            // Variable declaration code:
            // {evaluate expression, should place value on stack}
            // Instruction.SetVariable
            // {index of the variable}
            // Pop (for the value of the expression)

            var code = new CodeBlock();

            var currentScope = scopeManager.GetCurrentScope();

            // make sure an existing identifier does not already exist
            CodeGenHelper.ThrowIfSymbolAlreadyExists(vm, currentScope, _variableName);

            var index = currentScope.AddSymbol(_variableName, SymbolType.Numerical);

            code.Add(_valueExpression.Generate(vm, scopeManager));
            code.Add(Instruction.SetVariable);
            code.Add(index);
            code.Add(Instruction.Pop);

            return code;
        }

        public override void EmitTo(ICodeBlock codeBlock, IVirtualMachine vm, IScopeManager scopeManager)
        {
            var code = Generate(vm, scopeManager);

            codeBlock.Add(code);
        }
    }
}
