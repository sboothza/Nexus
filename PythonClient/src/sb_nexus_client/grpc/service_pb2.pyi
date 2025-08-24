from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional

DESCRIPTOR: _descriptor.FileDescriptor

class QueryRequest(_message.Message):
    __slots__ = ("bindingName", "extraInfo", "data")
    class ExtraInfoEntry(_message.Message):
        __slots__ = ("key", "value")
        KEY_FIELD_NUMBER: _ClassVar[int]
        VALUE_FIELD_NUMBER: _ClassVar[int]
        key: str
        value: str
        def __init__(self, key: _Optional[str] = ..., value: _Optional[str] = ...) -> None: ...
    BINDINGNAME_FIELD_NUMBER: _ClassVar[int]
    EXTRAINFO_FIELD_NUMBER: _ClassVar[int]
    DATA_FIELD_NUMBER: _ClassVar[int]
    bindingName: str
    extraInfo: _containers.ScalarMap[str, str]
    data: str
    def __init__(self, bindingName: _Optional[str] = ..., extraInfo: _Optional[_Mapping[str, str]] = ..., data: _Optional[str] = ...) -> None: ...

class QueryResponse(_message.Message):
    __slots__ = ("success", "extraInfo", "data")
    class ExtraInfoEntry(_message.Message):
        __slots__ = ("key", "value")
        KEY_FIELD_NUMBER: _ClassVar[int]
        VALUE_FIELD_NUMBER: _ClassVar[int]
        key: str
        value: str
        def __init__(self, key: _Optional[str] = ..., value: _Optional[str] = ...) -> None: ...
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    EXTRAINFO_FIELD_NUMBER: _ClassVar[int]
    DATA_FIELD_NUMBER: _ClassVar[int]
    success: bool
    extraInfo: _containers.ScalarMap[str, str]
    data: str
    def __init__(self, success: bool = ..., extraInfo: _Optional[_Mapping[str, str]] = ..., data: _Optional[str] = ...) -> None: ...

class Empty(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...
