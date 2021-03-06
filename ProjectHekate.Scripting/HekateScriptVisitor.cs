﻿using System;
using System.Collections.Generic;
using System.Linq;
using ProjectHekate.Grammar;
using ProjectHekate.Scripting.Bytecode.Emitters;
using ProjectHekate.Scripting.Bytecode.Generators;
using ProjectHekate.Scripting.Interfaces;

namespace ProjectHekate.Scripting
{
    public class HekateScriptVisitor : HekateBaseVisitor<AbstractBytecodeEmitter>
    {
        // These two stack's of lists are used to replace the dummy values for jumps/continues
        //  with the actual size of the enclosing loop constructs they should be in
        // When a loop construct 'begins', it should add an empty list to each of these
        // When a loop construct completes adding its code in its entirety, it should:
        //  1. Pop the top of the stack
        //  2. Iterate over each list and replace code[location] with the appropriate jump offset
        private readonly Stack<List<int>> _breakLocations;
        private readonly Stack<List<int>> _continueLocations;

        public HekateScriptVisitor()
        {
            _breakLocations = new Stack<List<int>>();
            _continueLocations = new Stack<List<int>>();
        }

        #region Top-level constructs

        public override AbstractBytecodeEmitter VisitScript(HekateParser.ScriptContext context)
        {
            var topLevelEmitters = new List<IBytecodeEmitter>();

            foreach (var child in context.children)
            {
                // visit each child and append the code to the main record
                var childEmitter = Visit(child);
                if(childEmitter == null) throw new InvalidOperationException("A visit to a child resulted in a null return value; check the visitor and make sure it overrides " + child.GetType().Name + "\'s visit method.");

                topLevelEmitters.Add(childEmitter);
            }

            return new ScriptEmitter(topLevelEmitters); // TODO: WHAT THE FUCK?
        }

        public override AbstractBytecodeEmitter VisitEmitterUpdaterDeclaration(HekateParser.EmitterUpdaterDeclarationContext context)
        {
            var paramNames = new List<string>();

            var paramList = context.formalParameters().formalParameterList();
            if (paramList != null) {
                var paramContexts = paramList.formalParameter();

                paramNames.AddRange(paramContexts.Select(fpc => fpc.NormalIdentifier().GetText()));
            }

            var name = context.NormalIdentifier().GetText();
            var statements = context.children.Select(Visit).Cast<IBytecodeEmitter>().ToList();
            
            return new EmitterUpdaterDeclarationStatementEmitter(paramNames, name, statements);
        }

        public override AbstractBytecodeEmitter VisitActionDeclaration(HekateParser.ActionDeclarationContext context)
        {
            var paramNames = new List<string>();

            var paramList = context.formalParameters().formalParameterList();
            if (paramList != null)
            {
                var paramContexts = paramList.formalParameter();

                paramNames.AddRange(paramContexts.Select(fpc => fpc.NormalIdentifier().GetText()));
            }

            var name = context.NormalIdentifier().GetText();

            var statements = new List<IBytecodeEmitter> {Visit(context.formalParameters()), Visit(context.updaterBody())};

            return new ActionDeclarationStatementEmitter(paramNames, name, statements);
        }

        public override AbstractBytecodeEmitter VisitFunctionDeclaration(HekateParser.FunctionDeclarationContext context)
        {
            var paramNames = new List<string>();

            var paramList = context.formalParameters().formalParameterList();
            if (paramList != null)
            {
                var paramContexts = paramList.formalParameter();

                paramNames.AddRange(paramContexts.Select(fpc => fpc.NormalIdentifier().GetText()));
            }

            var name = context.NormalIdentifier().GetText();
            var statements = context.children.Select(Visit).Cast<IBytecodeEmitter>().ToList();

            return new FunctionDeclarationStatementEmitter(paramNames, name, statements);
        }

        #endregion
      
  
        #region Statement constructs

        public override AbstractBytecodeEmitter VisitExpressionStatement(HekateParser.ExpressionStatementContext context)
        {
            var expGen = Visit(context.expression()); // all expressions should leave a single value on the stack
            var expStatementEmitter = new ExpressionStatementEmitter(expGen);

            return expStatementEmitter;
        }

        public override AbstractBytecodeEmitter VisitVariableDeclaration(HekateParser.VariableDeclarationContext context)
        {
            var valueExpression = Visit(context.expression());
            var variableName = context.NormalIdentifier().GetText();

            return new VariableDeclarationStatementEmitter(valueExpression, variableName);
        }

        public override AbstractBytecodeEmitter VisitReturnStatement(HekateParser.ReturnStatementContext context)
        {
            var expressionGen = Visit(context.expression());
            
            return new ReturnStatementEmitter(expressionGen);
        }

        public override AbstractBytecodeEmitter VisitIfStatement(HekateParser.IfStatementContext context)
        {
            var ifBodyStatementGen = Visit(context.statement(0));
            var elseBodyStatement = context.statement(1);
            var elseBodyStatementGen = elseBodyStatement == null ? null : Visit(elseBodyStatement);
            var ifConditionGen = Visit(context.parExpression());

            return new IfStatementEmitter(ifBodyStatementGen, elseBodyStatementGen, ifConditionGen);
        }

        public override AbstractBytecodeEmitter VisitForStatement(HekateParser.ForStatementContext context)
        {
            var breakList = new List<int>();
            _breakLocations.Push(breakList);

            var continueList = new List<int>();
            _continueLocations.Push(continueList);

            var forInitCtx = context.forInit();
            var forCondCtx = context.expression();
            var forUpdateCtx = context.forUpdate();
            
            var forInitGen = forInitCtx == null ? null : Visit(forInitCtx);
            var forCondGen = forCondCtx == null ? null : Visit(forCondCtx);
            var forUpdateGen = forUpdateCtx == null ? null : Visit(forUpdateCtx);
            var bodyStatementGen = Visit(context.statement());

            return new ForStatementEmitter(forInitGen, forCondGen, forUpdateGen, bodyStatementGen, breakList, continueList);
        }    

        public override AbstractBytecodeEmitter VisitWhileStatement(HekateParser.WhileStatementContext context)
        {
            var breakList = new List<int>();
            _breakLocations.Push(breakList);

            var continueList = new List<int>();
            _continueLocations.Push(continueList);

            var conditionExpressionGen = Visit(context.parExpression());
            var whileBodyStatementGen = Visit(context.statement());
            
            return new WhileStatementEmitter(conditionExpressionGen, whileBodyStatementGen, breakList, continueList);
        }

        public override AbstractBytecodeEmitter VisitBreakStatement(HekateParser.BreakStatementContext context)
        {
            var breakList = _breakLocations.Any() ? _breakLocations.Peek() : new List<int>();

            return new BreakStatementEmitter(breakList);
        }

        public override AbstractBytecodeEmitter VisitContinueStatement(HekateParser.ContinueStatementContext context)
        {
            var continueList = _continueLocations.Any() ? _continueLocations.Peek() : new List<int>();

            return new ContinueStatementEmitter(continueList);
        }

        public override AbstractBytecodeEmitter VisitWaitStatement(HekateParser.WaitStatementContext context)
        {
            var expressionEmitter = Visit(context.expression());

            return new WaitStatementEmitter(expressionEmitter);
        }

        public override AbstractBytecodeEmitter VisitFireStatement(HekateParser.FireStatementContext context)
        {
            var updater = context.withUpdaterOption();
            AbstractBytecodeEmitter updaterCallBytecodeGenerator = null;
            var expressionList = context.parExpressionList().expressionList();
            var numParams = expressionList == null ? 0 : expressionList.expression().Count;
            var numParamsOnUpdater = 0;
            var updaterName = "";

            if (updater != null) {
                updaterCallBytecodeGenerator = Visit(updater.updaterCallExpression());

                var updaterExpressionList = updater.updaterCallExpression().parExpressionList().expressionList();
                numParamsOnUpdater = updaterExpressionList == null ? 0 : updaterExpressionList.expression().Count;
                updaterName = updater.updaterCallExpression().NormalIdentifier().GetText();
            }

            // regular fire statement = [push parameters on stack], Fire, index
            // fire with updater stmt = [push parameters on stack], [push updater parameters on stack], FireWithUpdater, index, func index

            var parameterBytecodeGenerator = Visit(context.parExpressionList());
            return new FireStatementEmitter(context.TypeName.Text, context.FiringFunctionName.Text, numParams, parameterBytecodeGenerator, updaterCallBytecodeGenerator, numParamsOnUpdater, updaterName);
        }

        #endregion


        #region Miscellaneous statements (usually ones that wrap around expressions)

        public override AbstractBytecodeEmitter VisitEmptyStatement(HekateParser.EmptyStatementContext context)
        {
            return new EmptyEmitter();
        }

        public override AbstractBytecodeEmitter VisitVariableDeclarationStatement(HekateParser.VariableDeclarationStatementContext context)
        {
            var varDeclarationGen = Visit(context.variableDeclaration());

            return varDeclarationGen;
        }

        public override AbstractBytecodeEmitter VisitParenthesizedExpression(HekateParser.ParenthesizedExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override AbstractBytecodeEmitter VisitParExpression(HekateParser.ParExpressionContext context)
        {
            return Visit(context.expression());
        }

        public override AbstractBytecodeEmitter VisitParExpressionList(HekateParser.ParExpressionListContext context)
        {
            return context.expressionList() == null ? new EmptyEmitter() : Visit(context.expressionList());
        }

        public override AbstractBytecodeEmitter VisitExpressionList(HekateParser.ExpressionListContext context)
        {
            var expressionList = context.expression().Select(Visit).Cast<IBytecodeGenerator>().ToList();

            return new ExpressionListGenerator(expressionList);
        }

        public override AbstractBytecodeEmitter VisitBlockStatement(HekateParser.BlockStatementContext context)
        {
            return Visit(context.block());
        }

        public override AbstractBytecodeEmitter VisitBlock(HekateParser.BlockContext context)
        {
            var statementEmitters = context.statement().Select(Visit).Cast<IBytecodeEmitter>().ToList();

            return new BlockEmitter(statementEmitters);
        }

        public override AbstractBytecodeEmitter VisitForInit(HekateParser.ForInitContext context)
        {
            var isVariableDeclaration = context.variableDeclaration() != null;

            return isVariableDeclaration ? Visit(context.variableDeclaration()) : Visit(context.expressionList());
        }

        public override AbstractBytecodeEmitter VisitFormalParameters(HekateParser.FormalParametersContext context)
        {
            return context.formalParameterList() == null ? new EmptyEmitter() : Visit(context.formalParameterList());
        }

        public override AbstractBytecodeEmitter VisitFormalParameterList(HekateParser.FormalParameterListContext context)
        {
            var parameterList = context.formalParameter().Select(Visit).Cast<IBytecodeGenerator>().ToList();

            return new ExpressionListGenerator(parameterList);
        }

        public override AbstractBytecodeEmitter VisitUpdaterBody(HekateParser.UpdaterBodyContext context)
        {
            return Visit(context.block());
        }

        public override AbstractBytecodeEmitter VisitWithUpdaterOption(HekateParser.WithUpdaterOptionContext context)
        {
            return Visit(context.updaterCallExpression());
        }

        public override AbstractBytecodeEmitter VisitUpdaterCallExpression(HekateParser.UpdaterCallExpressionContext context)
        {
            var parameterExpressionGenerator = Visit(context.parExpressionList());

            return parameterExpressionGenerator;
        }

        #endregion


        #region Expression constructs

        public override AbstractBytecodeEmitter VisitLiteralExpression(HekateParser.LiteralExpressionContext context)
        {
            var text = context.GetText();
            var value = float.Parse(text);

            return new LiteralExpressionGenerator(value);
        }

        public override AbstractBytecodeEmitter VisitNormalIdentifierExpression(HekateParser.NormalIdentifierExpressionContext context)
        {
            var identifierName = context.NormalIdentifier().GetText();
            identifierName = CoaxIdentifierToProperName(IdentifierType.Variable, identifierName);

            return new NormalIdentifierExpressionGenerator(identifierName);
        }

        public override AbstractBytecodeEmitter VisitPropertyIdentifierExpression(HekateParser.PropertyIdentifierExpressionContext context)
        {
            var identifierName = context.PropertyIdentifier().GetText();
            identifierName = CoaxIdentifierToProperName(IdentifierType.Property, identifierName);

            return new PropertyIdentifierExpressionGenerator(identifierName);
        }

        public override AbstractBytecodeEmitter VisitPostIncDecExpression(HekateParser.PostIncDecExpressionContext context)
        {
            var isNormalIdentifier = context.NormalIdentifier() != null;
            var isPropertyIdentifier = context.PropertyIdentifier() != null;

            IdentifierType identifierType;
            string identifierName;
            if (isNormalIdentifier)
            {
                identifierType = IdentifierType.Variable;
                identifierName = context.NormalIdentifier().GetText();
            }
            else if (isPropertyIdentifier)
            {
                identifierType = IdentifierType.Property;
                identifierName = context.PropertyIdentifier().GetText();
            }
            else {
                throw new InvalidOperationException(
                    "You forgot to add a case for another identifier type! Check the code for VisitPostIncDecExpression.");
            }

            identifierName = CoaxIdentifierToProperName(identifierType, identifierName);

            var op = GetIncOrDecOperatorFromContext(context);
            return new PostIncDecExpressionGenerator(identifierType, identifierName, op);
        }

        public override AbstractBytecodeEmitter VisitAssignmentExpression(HekateParser.AssignmentExpressionContext context)
        {
            var isNormalIdentifier = context.NormalIdentifier() != null;
            var isPropertyIdentifier = context.PropertyIdentifier() != null;

            // determine whether its a variable or a property
            IdentifierType identifierType;
            string identifierName;
            if (isNormalIdentifier)
            {
                identifierType = IdentifierType.Variable;
                identifierName = context.NormalIdentifier().GetText();
            }
            else if (isPropertyIdentifier)
            {
                identifierType = IdentifierType.Property;
                identifierName = context.PropertyIdentifier().GetText();
            }
            else
            {
                throw new InvalidOperationException("You forgot to add a case for another identifier type! Check the code for VisitAssignmentExpression.");
            }

            identifierName = CoaxIdentifierToProperName(identifierType, identifierName);

            var exprGen = Visit(context.expression());
            if (context.Operator.Type == HekateParser.ASSIGN) {
                return new SimpleAssignmentExpressionGenerator(exprGen, identifierType, identifierName);
            }
            else {
                var op = GetCompoundAssignmentOperatorFromContext(context);
                return new CompoundAssignmentExpressionGenerator(exprGen, identifierType, identifierName, op);
            }
        }

        public override AbstractBytecodeEmitter VisitFunctionCallExpression(HekateParser.FunctionCallExpressionContext context)
        {
            var functionName = context.NormalIdentifier().GetText();
            
            return new FunctionCallExpressionGenerator(Visit(context.parExpressionList()), functionName);
        }

        public override AbstractBytecodeEmitter VisitUnaryExpression(HekateParser.UnaryExpressionContext context)
        {
            var op = GetUnaryOperatorFromContext(context);

            return new UnaryExpressionGenerator(Visit(context.expression()), op);
        }

        public override AbstractBytecodeEmitter VisitBinaryExpression(HekateParser.BinaryExpressionContext context)
        {
            var leftExprGen = Visit(context.expression(0));
            var rightExprGen = Visit(context.expression(1));
            var op = GetBinaryOperatorFromContext(context);

            return new BinaryExpressionGenerator(leftExprGen, rightExprGen, op);
        }

        public override AbstractBytecodeEmitter VisitTernaryOpExpression(HekateParser.TernaryOpExpressionContext context)
        {
            var testExprGen = Visit(context.expression(0));
            var ifTrueExprGen = Visit(context.expression(1));
            var ifFalseExprGen = Visit(context.expression(2));

            return new TernaryOpExpressionGenerator(testExprGen, ifTrueExprGen, ifFalseExprGen);
        }

        private Instruction GetIncOrDecOperatorFromContext(HekateParser.PostIncDecExpressionContext context)
        {
            switch (context.Operator.Type)
            {
                case HekateParser.INC: return Instruction.OpAdd;
                case HekateParser.DEC: return Instruction.OpSubtract;
                default: throw new InvalidOperationException("Invalid operator type found for this PostIncDecExpressionContext (" + context.Operator.Text + ").");
            }
        }

        private Instruction GetUnaryOperatorFromContext(HekateParser.UnaryExpressionContext context)
        {
            switch (context.Operator.Type)
            {
                case HekateParser.SUB:      return Instruction.Negate;
                case HekateParser.BANG:     return Instruction.OpNot;
                default:                    throw new InvalidOperationException("You forgot to add support for an operator! Check the code for support for the " + context.Operator.Text + " operator.");
            }
        }

        private Instruction GetBinaryOperatorFromContext(HekateParser.BinaryExpressionContext context)
        {
            switch (context.Operator.Type) {
                case HekateParser.MUL:      return Instruction.OpMultiply;
                case HekateParser.DIV:      return Instruction.OpDivide;
                case HekateParser.MOD:      return Instruction.OpMod;
                case HekateParser.ADD:      return Instruction.OpAdd;
                case HekateParser.SUB:      return Instruction.OpSubtract;
                case HekateParser.LT:       return Instruction.OpLessThan;
                case HekateParser.GT:       return Instruction.OpGreaterThan;
                case HekateParser.LE:       return Instruction.OpLessThanEqual;
                case HekateParser.GE:       return Instruction.OpGreaterThanEqual;
                case HekateParser.EQUAL:    return Instruction.OpEqual;
                case HekateParser.NOTEQUAL: return Instruction.OpNotEqual;
                case HekateParser.AND:      return Instruction.OpAnd;
                case HekateParser.OR:       return Instruction.OpOr;
                default:                    throw new InvalidOperationException("You forgot to add support for an operator! Check the code for support for the " + context.Operator.Text + " operator.");
            }
        }

        private Instruction GetCompoundAssignmentOperatorFromContext(HekateParser.AssignmentExpressionContext context)
        {
            switch (context.Operator.Type) {
                case HekateParser.MUL_ASSIGN:   return Instruction.OpMultiply;
                case HekateParser.DIV_ASSIGN:   return Instruction.OpDivide;
                case HekateParser.ADD_ASSIGN:   return Instruction.OpAdd;
                case HekateParser.SUB_ASSIGN:   return Instruction.OpSubtract;
                default:
                    throw new InvalidOperationException(
                        "You forgot to add a case for a compound assignment operator! Check the code for GetCompoundAssignmentOperatorFromContext.");
            }
        }

        public string CoaxIdentifierToProperName(IdentifierType identifierType, string identifierName)
        {
            switch (identifierType)
            {
                case IdentifierType.Property:
                    return identifierName.FirstOrDefault() == '$' ? identifierName.Substring(1) : identifierName; // get rid of the first character ($)
                case IdentifierType.Variable:
                    // dont need to coax variable identifierName to anything
                    return identifierName;
                default:
                    throw new ArgumentOutOfRangeException("identifierType");
            }
        }
        #endregion
    }
}
