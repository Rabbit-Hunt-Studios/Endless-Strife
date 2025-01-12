using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TbsFramework.Units;

public class ESUnit : Unit
{
    
    public override void MarkAsDefending(Unit aggressor) {}

    public override void MarkAsAttacking(Unit target) {}

    public override void MarkAsDestroyed() {}

    public override void MarkAsFriendly() {}
    
    public override void MarkAsReachableEnemy() {}

    public override void MarkAsSelected() {}

    public override void MarkAsFinished() {}

    public override void UnMark() {}
}
