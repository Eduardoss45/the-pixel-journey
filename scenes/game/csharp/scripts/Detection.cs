using System;
using Godot;

public partial class Detection : Area2D
{
    [Export]
    public NodePath BossPath = new NodePath("Objects/Boss");

    [Export]
    public bool ActivateOnEnter = true;

    [Export]
    public bool ActivateValue = true;

    [Export]
    public bool OnlyOnce = true;

    private bool _activated;
    private Boss _boss;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        _boss = ResolveBoss();
    }

    private Boss ResolveBoss()
    {
        if (BossPath != null && !BossPath.IsEmpty)
        {
            var byPath = GetNodeOrNull<Boss>(BossPath);
            if (byPath != null)
                return byPath;
        }

        return GetTree().GetFirstNodeInGroup("Boss") as Boss;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (OnlyOnce && _activated)
            return;

        if (body is not Player)
            return;

        if (_boss == null)
            _boss = ResolveBoss();
        if (_boss == null)
            return;

        if (ActivateOnEnter)
            _boss.SetActive(ActivateValue);

        _activated = true;
    }
}
