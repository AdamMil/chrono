using System;
using System.Xml;

namespace Chrono
{

public abstract class Shield : Wieldable
{ public Shield() { Class=ItemClass.Shield; }

  public override string GetFullName(bool forceSingular)
  { if(!Identified) return base.GetFullName(forceSingular);
    string status = StatusString;
    if(status!="") status += ' ';
    string ret = status + (AC<0 ? "" : "+") + AC + ' ' + Name;
    if(Title!=null) ret += " named "+Title;
    return ret;
  }

  public int BlockChance; // base percentage chance that this shield will block a blow
}

#region XmlShield
public sealed class XmlShield : Shield
{ public XmlShield() { }
  public XmlShield(XmlNode node)
  { XmlItem.InitModifying(this, node);
    BlockChance = Xml.Int(node, "blockChance");
  }
}
#endregion

} // namespace Chrono