using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TbsFramework.Units;
using TbsFramework.Cells;
using EndlessStrife.Cells;
using System;

public class ESUnit : Unit
{
    public Sprite Player1Sprite;
    public Sprite Player2Sprite;
    public Sprite DefaultSprite;
    public Sprite UnitWeaponIcon;
    public string UnitName;
    public string UnitUnlock;
    public Vector3 Offset;
    public bool isStructure;
    private bool preview = true;

    public override void Initialize()
    {
        base.Initialize();
        transform.localPosition += Offset;
    }

    public override void MarkAsDestroyed()
    {
    }

    protected override int Defend(Unit other, int damage)
    {
        return (int)(damage * (100 - DefenceFactor) * 0.01) - (Cell as ESSquare).DefenceBoost;
    }

    protected override AttackAction DealDamage(Unit unitToAttack)
    {
        var baseVal = base.DealDamage(unitToAttack);
        var newDmg = TotalHitPoints == 0 ? 0 : (int)Mathf.Ceil(baseVal.Damage);

        return new AttackAction(newDmg, baseVal.ActionCost);
    }

    public override IEnumerator Move(Cell destinationCell, IList<Cell> path)
    {
        GetComponent<SpriteRenderer>().sortingOrder += 10;
        transform.Find("Marker").GetComponent<SpriteRenderer>().sortingOrder += 10;
        yield return base.Move(destinationCell, path);
    }

    protected override void OnMoveFinished()
    {
        GetComponent<SpriteRenderer>().sortingOrder -= 10;
        transform.Find("Marker").GetComponent<SpriteRenderer>().sortingOrder -= 10;
        base.OnMoveFinished();
    }

    public override bool IsCellTraversable(Cell cell)
    {
        return base.IsCellTraversable(cell) || (cell.CurrentUnits.Count > 0 && !cell.CurrentUnits.Exists(u => !(u as ESUnit).isStructure && u.PlayerNumber != PlayerNumber));
    }

    public override void SetColor(Color color)
    {
        var highlighter = transform.Find("Marker");
        var spriteRenderer = highlighter.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    public void turn_on_preview()
    {
        preview = true;
    }

    public override void OnMouseDown()
    {
        if (!this.isStructure && preview)
        {
            this.GetComponent<StatsDisplayAbility>().destroy_cards();
        }
        
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            base.OnMouseDown();
        }
        preview = false;
    }

    public override void OnMouseEnter()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            base.OnMouseEnter();
        }
        if (!this.isStructure && preview)
        {
            this.GetComponent<StatsDisplayAbility>().create_stat_panel();
        }
        Cell.MarkAsHighlighted();
    }

    public override void OnMouseExit()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            base.OnMouseExit();
        }
        if (!this.isStructure && preview)
        {
            this.GetComponent<StatsDisplayAbility>().destroy_cards();
        }
        Cell.UnMark();
    }
}
