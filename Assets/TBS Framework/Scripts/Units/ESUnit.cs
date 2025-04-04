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
    public enum AttackSoundType
    {
        Slashed,
        Bow,
        Shoot,
        Magic,
        None
    }

    public Sprite Player1Sprite;
    public Sprite Player2Sprite;
    public Sprite DefaultSprite;
    public Sprite UnitWeaponIcon;
    public string UnitName;
    public string UnitUnlock;
    public Vector3 Offset;
    public bool isStructure;
    private List<AudioClip> UnitAttackSounds;
    private AudioController audioController;
    public AttackSoundType SelectedAttackSound = AttackSoundType.Slashed;

    public override void Initialize()
    {
        base.Initialize();
        transform.localPosition += Offset;
        audioController = GameObject.Find("AudioController").GetComponent<AudioController>();
        UnitAttackSounds = new List<AudioClip>();
        UnitAttackSounds.Add(audioController.UnitSlashed);
        UnitAttackSounds.Add(audioController.UnitBow);
        UnitAttackSounds.Add(audioController.UnitShoot);
        UnitAttackSounds.Add(audioController.UnitMagic);
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
        if (newDmg > 0 && SelectedAttackSound != AttackSoundType.None && !audioController.SFXSource.mute)
        {
            audioController.PlaySFX(UnitAttackSounds[(int)SelectedAttackSound]);
        }

        return new AttackAction(newDmg, baseVal.ActionCost);
    }

    public override IEnumerator Move(Cell destinationCell, IList<Cell> path)
    {
        audioController.PlaySFX(audioController.UnitMove);
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

    public override bool IsUnitAttackable(Unit other, Cell otherCell, Cell sourceCell)
    {
        return base.IsUnitAttackable(other, otherCell, sourceCell) && other.GetComponent<CapturableAbility>() == null;
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

    public override void OnMouseDown()
    {
        if (!this.isStructure)
        {
            this.GetComponent<StatsDisplayAbility>().destroy_cards();
        }
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            base.OnMouseDown();
        }
    }

    public override void OnMouseEnter()
    {
        audioController.PlaySFX(audioController.ButtonHover);
        if (!this.isStructure)
        {
            this.GetComponent<StatsDisplayAbility>().create_stat_panel();
        }
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            base.OnMouseEnter();
        }
        Cell.MarkAsHighlighted();
    }

    public override void OnMouseExit()
    {
        if (!this.isStructure)
        {
            this.GetComponent<StatsDisplayAbility>().destroy_cards();
        }
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            base.OnMouseExit();
        }
        Cell.UnMark();
    }
}
