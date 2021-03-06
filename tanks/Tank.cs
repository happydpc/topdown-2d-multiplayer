using Godot;
using System;

public class Tank : KinematicBody2D
{


    [Signal]
    public delegate void HealthChangedSignal();

    [Signal]
    public delegate void DefeatedAgentChangedSignal();

    [Signal]
    public delegate void DeadSignal();

    [Signal]
    public delegate void PrimaryWeaponChangeSignal();

    [Signal]
    public delegate void SecondaryWeaponChangeSignal();

    [Export]
    protected int MaxSpeed;

    [Export]
    protected float RotationSpeed;

    [Export]
    protected int MaxHealth;

    [Export]
    protected int MaxEnergy;

    public Vector2 Velocity;
    protected Boolean Alive = true;

    public float currentTime = 0;

    public int currentPrimaryWeaponIndex { get; set; }
    public int currentSecondaryWeaponIndex { get; set; }

    protected Godot.Collections.Array primaryWeapons = new Godot.Collections.Array();
    protected Godot.Collections.Array secondaryWeapons = new Godot.Collections.Array();

    [Export]
    private int MaxPrimaryWeaponCount = 3;

    [Export]
    private int MaxSecondaryWeaponCount = 1;


    [Export]
    private String unitName = "Default";

    private int health;

    private int energy;

    protected AudioStream explosionMusicClip = (AudioStream)GD.Load("res://assets/sounds/explosion_large_07.wav");
    protected AudioStream moveMusicClip = (AudioStream)GD.Load("res://assets/sounds/sci-fi_device_item_power_up_flash_01.wav");

    protected GameStates gameStates;
    protected Network network;

    protected Node2D target = null;

    private String teamIdentifier = "UNKOWN";

    protected GameWorld gameworld { get; set; }

    private int defeatedAgentCount = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        gameStates = (GameStates)GetNode("/root/GAMESTATES");
        gameworld = (GameWorld)GetParent();
        network = (Network)GetNode("/root/NETWORK");

        health = MaxHealth;
        energy = MaxEnergy;

        Particles2D smoke = (Particles2D)GetNode("Smoke");
        smoke.Emitting = false;

        EmitSignal(nameof(HealthChangedSignal), health * 100 / MaxHealth);

        currentPrimaryWeaponIndex = -1;
        currentSecondaryWeaponIndex = -1;

        // Temporary script to automatic load weapon
        updatePrimaryWeapon((PackedScene)GD.Load("res://weapons/LaserGun.tscn"));
        updatePrimaryWeapon((PackedScene)GD.Load("res://weapons/MissleLauncher.tscn"));
        updatePrimaryWeapon((PackedScene)GD.Load("res://weapons/Rifile.tscn"));
    }

    public void changePrimaryWeapon(int weaponIndex)
    {
        if (currentPrimaryWeaponIndex != -1)
        {
            ((Weapon)primaryWeapons[currentPrimaryWeaponIndex]).Hide();
            currentPrimaryWeaponIndex = weaponIndex % primaryWeapons.Count;
            if (currentPrimaryWeaponIndex < 0) { currentPrimaryWeaponIndex = currentPrimaryWeaponIndex * -1; }
            ((Weapon)primaryWeapons[currentPrimaryWeaponIndex]).Show();
            EmitSignal(nameof(PrimaryWeaponChangeSignal), ((Weapon)primaryWeapons[currentPrimaryWeaponIndex]).weaponType);
        }
    }

    public void changeSecondaryWeapon(int weaponIndex)
    {
        if (currentSecondaryWeaponIndex != -1)
        {
            ((Weapon)secondaryWeapons[currentSecondaryWeaponIndex]).Hide();
            currentSecondaryWeaponIndex = weaponIndex % secondaryWeapons.Count;
            if (currentSecondaryWeaponIndex < 0) { currentSecondaryWeaponIndex = currentSecondaryWeaponIndex * -1; }
            ((Weapon)secondaryWeapons[currentSecondaryWeaponIndex]).Show();
            EmitSignal(nameof(SecondaryWeaponChangeSignal), ((Weapon)secondaryWeapons[currentSecondaryWeaponIndex]).weaponType);
        }

    }
    public bool updatePrimaryWeapon(PackedScene weapon)
    {
        if (primaryWeapons.Count < MaxPrimaryWeaponCount)
        {
            // Hide existing weapon if exist
            if (currentPrimaryWeaponIndex != -1)
            {
                ((Weapon)primaryWeapons[currentPrimaryWeaponIndex]).Hide();
            }

            Position2D weaponHolder = ((Position2D)GetNode("Weapon"));
            Weapon curretnWeapon = (Weapon)(weapon.Instance());
            curretnWeapon.gameWorld = gameworld;
            primaryWeapons.Add(curretnWeapon);
            // Update to use this weapon as primary
            currentPrimaryWeaponIndex = primaryWeapons.Count - 1;
            weaponHolder.AddChild(curretnWeapon);
            EmitSignal(nameof(PrimaryWeaponChangeSignal), ((Weapon)primaryWeapons[currentPrimaryWeaponIndex]).weaponType);
            return true;
        }

        return false;
    }

    public bool updateSecondaryWeapon(PackedScene weapon)
    {
        if (secondaryWeapons.Count < MaxSecondaryWeaponCount)
        {
            // Hide existing weapon if exist
            if (currentSecondaryWeaponIndex != -1)
            {
                ((Weapon)secondaryWeapons[currentSecondaryWeaponIndex]).Hide();
            }

            Position2D weaponHolder = ((Position2D)GetNode("Weapon"));
            Weapon curretnWeapon = (Weapon)(weapon.Instance());
            curretnWeapon.gameWorld = gameworld;
            secondaryWeapons.Add(curretnWeapon);
            // Update to use this weapon as secondary
            currentSecondaryWeaponIndex = secondaryWeapons.Count - 1;
            weaponHolder.AddChild(curretnWeapon);
            EmitSignal(nameof(SecondaryWeaponChangeSignal), ((Weapon)secondaryWeapons[currentSecondaryWeaponIndex]).weaponType);

            return true;
        }

        return false;
    }

    public void setTeamIdentifier(String inputTeamIdentifier)
    {
        teamIdentifier = inputTeamIdentifier;
        ((Label)(GetNode("UnitDisplay/Name"))).Text = unitName + "(" + teamIdentifier + ")";
    }

    public String getTeamIdentifier()
    {
        return teamIdentifier;
    }

    public void setUnitName(String unitName)
    {
        this.unitName = unitName;
        ((Label)(GetNode("UnitDisplay/Name"))).Text = unitName + "(" + teamIdentifier + ")";
    }

    public String getUnitName()
    {
        return unitName;
    }

    public virtual void _Control(float delta) { }

    public void move(Vector2 moveDir, Vector2 pointPosition, float delta)
    {
        if (moveDir.x == 0 && moveDir.y == 0)
        {
            slowDownBoostTrail();
        }
        else
        {
            speedUpBoostTrail();

            //AudioManager audioManager = (AudioManager)GetNode("/root/AUDIOMANAGER");
            //audioManager.playSoundEffect(moveMusicClip);

            // Need to times 100 to catch up with AI movement as there is delay in updating
            Velocity = moveDir.Normalized() * MaxSpeed * delta * 100;
            MoveAndSlide(Velocity);
        }


        LookAt(pointPosition);
    }

    protected void speedUpBoostTrail()
    {
        Particles2D boosterTrail = (Particles2D)GetNode("Partilcle2DBoosterTrail");

        boosterTrail.SpeedScale = 10;
        boosterTrail.Lifetime = 3;

    }

    protected void slowDownBoostTrail()
    {
        Particles2D boosterTrail = (Particles2D)GetNode("Partilcle2DBoosterTrail");

        boosterTrail.SpeedScale = 1;
        boosterTrail.Lifetime = 1;
    }


    public void set(Vector2 position, float rotation, bool primaryWeapon, bool secondaryWeapon, bool playerMove)
    {

        Particles2D boosterTrail = (Particles2D)GetNode("Partilcle2DBoosterTrail");
        Tween tween = (Tween)GetNode("Tween");

        // Move effect
        if (position != Position)
        {
            //AudioManager audioManager = (AudioManager)GetNode("/root/AUDIOMANAGER");
            //audioManager.playSoundEffect(moveMusicClip);

            speedUpBoostTrail();
        }
        else
        {
            slowDownBoostTrail();
        }

        _shoot(primaryWeapon, secondaryWeapon);

        Position = position;
        Rotation = rotation;
    }

    public void _shoot(bool primaryWeapon, bool secondaryWeapon)
    {
        if (primaryWeapon && currentPrimaryWeaponIndex != -1)
        {
            // knock back effect
            if (((Weapon)primaryWeapons[currentPrimaryWeaponIndex]).fire(this, target) && MaxSpeed != 0)
            {
                Vector2 dir = (new Vector2(1, 0)).Rotated(GlobalRotation);
                MoveAndSlide(dir * -1 * ((Weapon)primaryWeapons[currentPrimaryWeaponIndex]).KnockbackForce);
            }
        }

        if (secondaryWeapon && currentSecondaryWeaponIndex != -1)
        {
            // knock back effect
            if (((Weapon)secondaryWeapons[currentSecondaryWeaponIndex]).fire(this, target) && MaxSpeed != 0)
            {
                Vector2 dir = (new Vector2(1, 0)).Rotated(GlobalRotation);
                MoveAndSlide(dir * -1 * ((Weapon)secondaryWeapons[currentSecondaryWeaponIndex]).KnockbackForce);
            }
        }
    }

    public void setHealth(int health)
    {
        this.health = health;
        EmitSignal(nameof(HealthChangedSignal), health * 100 / MaxHealth);
    }

    public int getHealth()
    {
        return health;
    }

    public void incrementDefeatedAgentCount()
    {
        defeatedAgentCount++;
        EmitSignal(nameof(DefeatedAgentChangedSignal), defeatedAgentCount);
    }

    public int getDefeatedAgentCount()
    {
        return defeatedAgentCount;
    }

    public void TakeDamage(int amount, Vector2 dir, Tank source)
    {
        int originalHealth = health;
        bool trackDamage = true;
        bool sourceAlive = true;

        if (source == null || !IsInstanceValid(source))
        {
            sourceAlive = false;
        }

        if (source != null && IsInstanceValid(source) && source.getTeamIdentifier() == getTeamIdentifier())
        {
            trackDamage = false;
        }

        if (trackDamage)
        {
            health -= amount;

            EmitSignal(nameof(HealthChangedSignal), health * 100 / MaxHealth);

            if (health < MaxHealth / 2)
            {
                Particles2D smoke = (Particles2D)GetNode("Smoke");
                smoke.Emitting = true;
            }

            // Only the one that actually damange agent to 0 will be count as the one defeated
            if (originalHealth > 0 && health <= 0)
            {
                if (sourceAlive)
                {
                    source.incrementDefeatedAgentCount();
                }
            }
        }


        // knock back effect
        if (MaxSpeed != 0)
        {
            MoveAndSlide(dir * 50 * amount);
        }
    }


    public void heal(int amount)
    {

        health = +amount;

        if (health > MaxHealth)
        {
            health = MaxHealth;
        }

        if (health >= MaxHealth / 2)
        {
            Particles2D smoke = (Particles2D)GetNode("Smoke");
            smoke.Emitting = false;
        }
    }

    public bool ammoIncrease(Weapon.WeaponAmmoType weaponAmmoType, int amount)
    {
        bool consume = false;

        if (weaponAmmoType != Weapon.WeaponAmmoType.machine_energy)
        {
            foreach (Weapon currentWeapon in primaryWeapons)
            {
                if (weaponAmmoType == currentWeapon.weaponAmmoType)
                {
                    consume = true;
                    ((Weapon)primaryWeapons[currentPrimaryWeaponIndex]).ammoIncrease(amount);
                }
            }
        }
        else
        {
            energy = +amount;

            if (energy > MaxEnergy)
            {
                energy = MaxEnergy;
            }
        }

        return consume;
    }

    public void explode()
    {
        CollisionShape2D collisionShape2D = (CollisionShape2D)GetNode("CollisionShape2D");
        collisionShape2D.Disabled = true;
        Alive = false;
        Sprite body = (Sprite)GetNode("Body");
        body.Hide();

        AnimatedSprite animatedSprite = (AnimatedSprite)GetNode("Explosion");
        animatedSprite.Show();
        animatedSprite.Play("fire");

        AudioManager audioManager = (AudioManager)GetNode("/root/AUDIOMANAGER");
        audioManager.playSoundEffect(explosionMusicClip);
    }

    private void _OnExplosionAnimationFinished()
    {
        QueueFree();
        EmitSignal(nameof(DeadSignal));
    }

}
