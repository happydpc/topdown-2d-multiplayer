using Godot;
using System;

public class Pickup : Area2D
{
    enum Items { health, ammo }

    private Texture[] iconTextures = { (Texture)GD.Load("res://assets/wrench_white.png"), (Texture)GD.Load("res://assets/ammo_machinegun.png") };
    AudioStream musicClip = (AudioStream)GD.Load("res://assets/sounds/Future Weapons 2 - Source - ammunition_elements_13.wav");

    [Export]
    Items type = Items.health;

    [Export]
    Vector2 amount = new Vector2(10, 25);

    public override void _Ready()
    {
        Sprite icon = (Sprite)GetNode("Icon");
        icon.Texture = iconTextures[(int)type];
    }

    public void _onPickupBodyEntered(Node2D body)
    {


        AudioManager audioManager = (AudioManager)GetNode("/root/AUDIOMANAGER");
        audioManager.playSoundEffect(musicClip);

        if (type == Items.health)
        {
            if (body.HasMethod("heal"))
            {
                Tank tank = (Tank)body;
                tank.heal((int)GD.RandRange(amount.x, amount.y));

            }
        }
        if (type == Items.ammo)
        {
            if (body.HasMethod("ammoIncrease"))
            {
                Tank tank = (Tank)body;
                tank.ammoIncrease(Weapon.WeaponAmmoType.bullet,(int)GD.RandRange(amount.x, amount.y));

            }
        }
        QueueFree();
    }
}
