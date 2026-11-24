using ProtoBuf;
namespace SyntacticWizardry.Game;
[ProtoContract] public sealed class Request { [ProtoMember(1)] public string Action {get;set;}=""; [ProtoMember(2)] public string SpellJson {get;set;}=""; [ProtoMember(3)] public long Revision {get;set;} }
[ProtoContract] public sealed class State { [ProtoMember(1)] public string SpellJson {get;set;}=""; [ProtoMember(2)] public float Mana {get;set;} [ProtoMember(3)] public float MaxMana {get;set;} [ProtoMember(4)] public string Message {get;set;}=""; [ProtoMember(5)] public long Revision {get;set;} }
