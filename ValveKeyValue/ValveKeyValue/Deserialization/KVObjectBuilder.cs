﻿using System;
using System.Collections.Generic;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization
{
    class KVObjectBuilder : IParsingVisitationListener
    {
        readonly IList<KVObjectBuilder> associatedBuilders = new List<KVObjectBuilder>();

        public KVObject GetObject()
        {
            if (stateStack.Count != 1)
            {
                throw new KeyValueException($"Builder is not in a fully completed state (stack count is {stateStack.Count}).");
            }

            foreach (var associatedBuilder in associatedBuilders)
            {
                associatedBuilder.FinalizeState();
            }

            var state = stateStack.Peek();
            return MakeObject(state);
        }

        readonly Stack<KVPartialState> stateStack = new();

        public void OnKeyValuePair(string name, KVValue value)
        {
            if (StateStack.Count > 0)
            {
                var state = StateStack.Peek();
                state.Items.Add(new KVObject(name, value));
            }
            else
            {
                var state = new KVPartialState
                {
                    Key = name,
                    Value = value
                };

                StateStack.Push(state);
            }
        }

        public void OnArrayValue(KVValue value)
        {
            if (StateStack.Count > 0)
            {
                var state = StateStack.Peek();
                state.Children.Add(value);
            }
            else
            {
                var state = new KVPartialState
                {
                    Value = value
                };

                StateStack.Push(state);
            }
        }

        public void OnObjectEnd()
        {
            if (StateStack.Count <= 1)
            {
                return;
            }

            var state = StateStack.Pop();

            var completedObject = MakeObject(state);

            var parentState = StateStack.Peek();

            if (parentState.IsArray)
            {
                parentState.Children.Add(completedObject.Value); // TODO: Avoid wrapping it into KVObject in the first place?
            }
            else
            {
                parentState.Items.Add(completedObject);
            }
        }

        public void OnArrayEnd()
        {
            if (StateStack.Count <= 1)
            {
                return;
            }

            var state = StateStack.Pop();

            var completedObject = MakeArray(state);

            var parentState = StateStack.Peek();

            if (parentState.IsArray)
            {
                parentState.Children.Add(completedObject.Value); // TODO: Avoid wrapping it into KVObject in the first place?
            }
            else
            {
                parentState.Items.Add(completedObject);
            }
        }

        public void DiscardCurrentObject()
        {
            var state = StateStack.Peek();
            if (state.Items?.Count > 0)
            {
                state.Items.RemoveAt(state.Items.Count - 1);
            }
            else
            {
                StateStack.Pop();
            }
        }

        public void OnObjectStart(string name, KVFlag flag)
        {
            var state = new KVPartialState
            {
                Key = name,
                Flag = flag,
            };
            StateStack.Push(state);
        }

        public void OnArrayStart(string name, KVFlag flag)
        {
            var state = new KVPartialState
            {
                Key = name,
                Flag = flag,
                IsArray = true,
            };
            StateStack.Push(state);
        }

        public IParsingVisitationListener GetMergeListener()
        {
            var builder = new KVMergingObjectBuilder(this);
            associatedBuilders.Add(builder);
            return builder;
        }

        public IParsingVisitationListener GetAppendListener()
        {
            var builder = new KVAppendingObjectBuilder(this);
            associatedBuilders.Add(builder);
            return builder;
        }

        public void Dispose()
        {
        }

        internal Stack<KVPartialState> StateStack => stateStack;

        protected virtual void FinalizeState()
        {
            foreach (var associatedBuilder in associatedBuilders)
            {
                associatedBuilder.FinalizeState();
            }
        }

        KVObject MakeObject(KVPartialState state)
        {
            if (state.Discard)
            {
                return null;
            }

            if (state.IsArray)
            {
                throw new InvalidCastException("Tried to make an object ouf of an array.");
            }

            KVObject @object;

            if (state.Value != null)
            {
                @object = new KVObject(state.Key, state.Value);
            }
            else
            {
                @object = new KVObject(state.Key, state.Items);
            }

            @object.Value.Flag = state.Flag;

            return @object;
        }

        KVObject MakeArray(KVPartialState state)
        {
            if (state.Discard)
            {
                return null;
            }

            if (!state.IsArray)
            {
                throw new InvalidCastException("Tried to make an array out of an object.");
            }

            KVObject @object;

            if (state.Value != null)
            {
                @object = new KVObject(state.Key, state.Value);
            }
            else
            {
                @object = new KVObject(state.Key, state.Children);
            }

            @object.Value.Flag = state.Flag;

            return @object;
        }
    }
}
