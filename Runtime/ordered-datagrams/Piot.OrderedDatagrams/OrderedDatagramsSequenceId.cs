/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Piot.OrderedDatagrams
{
    /// <summary>
    ///     Very simple protocol to detect out of order and dropped datagrams.
    /// </summary>
    public struct OrderedDatagramsSequenceId
    {
        public OrderedDatagramsSequenceId(ushort sequenceId)
        {
            Value = sequenceId;
        }

        public void Next()
        {
            Value++;  
        } 

        public ushort Value { get; private set;  }

        public uint Diff(OrderedDatagramsSequenceId value)
        {
            int diff;

            var id = value.Value;
            if(Value < id)
            {
                diff = Value + 65536 - id;
            }
            else
            {
                diff = Value - id;
            }
            
            if (diff < 0)
            {
                throw new("delta is negative");
            }

            return (uint)diff;
        }

        public bool IsValidSuccessor(OrderedDatagramsSequenceId value)
        {
            var diff = Diff(value);


            return diff <= 32767;
        }
        
        public bool IsValidSuccessor(uint diff)
        {
            return diff <= 32767;
        }

        public override string ToString()
        {
            return $"[OrderedDatagramsSequenceId {Value}]";
        }
    }
}