using System;
using System.Linq;
using UnityEngine;
using FluxFramework.Attributes.VisualScripting;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Node
{
    public enum ComparisonOperation
    {
        Equal,
        NotEqual,
        Greater,
        GreaterEqual,
        Less,
        LessEqual
    }
}