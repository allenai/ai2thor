from typing import Protocol, Dict, Any, TYPE_CHECKING

if TYPE_CHECKING:
    from ai2thor.controller import Controller


class MetadataHook(Protocol):
    def __call__(self, metadata: Dict[str, Any], controller: "Controller"):
        ...
