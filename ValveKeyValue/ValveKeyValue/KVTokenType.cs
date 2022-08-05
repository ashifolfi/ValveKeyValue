﻿namespace ValveKeyValue
{
    enum KVTokenType
    {
        ObjectStart,
        ObjectEnd,
        String,
        EndOfFile,
        Comment,
        Condition,
        IncludeAndAppend,
        IncludeAndMerge,

        // KeyValues3
        Header,
        Assignment,
        CommentBlock,

        SEEK_VALUE,
        PROP_NAME,
        VALUE_STRUCT,
        VALUE_ARRAY,
        VALUE_STRING_MULTI,
        VALUE_NUMBER,
        VALUE_FLAGGED,
    }
}
