﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHekate.Scripting
{
    public enum Instructions : byte
    {
        Push,
        Pop,
        OperatorNegative,
        OperatorAdd,
        OperatorSubtract,
        OperatorMultiply,
        OperatorDivide,
        OperatorMod,
        OperatorLessThan,
        OperatorLessThanEqual,
        OperatorGreaterThan,
        OperatorGreaterThanEqual,
        OperatorEqual,
        OperatorNotEqual,
        OperatorAnd,
        OperatorOr,
        Assign,
        Jump,
        Compare,
        FunctionCall,
        GetUpdater,
        GetProperty,
        SetProperty,
        GetVariable,
        SetVariable,
        Fire,
        WaitFrames
    }
}
