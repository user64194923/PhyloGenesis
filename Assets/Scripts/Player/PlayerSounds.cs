using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSounds : MonoBehaviour {


    [SerializeField] private AudioSource FootstepsAudioSource;
    [SerializeField] private AudioSource GearAudioSource;
    [SerializeField] private AudioSource BulletSource;
    [SerializeField] private AudioSource ReloadSource;
    [SerializeField] private AudioSource PickUpSource;
    [SerializeField] private AudioSource EffectsSource;
    [SerializeField] private AudioSource LegPunchSource;

    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip[] gearSounds;
    [SerializeField] private AudioClip[] weaponSwapSounds;
    [SerializeField] private AudioClip[] bulletSounds;
    [SerializeField] private AudioClip AmmoPickUpSound;
    [SerializeField] private AudioClip HealthKitPickUpSound;
    [SerializeField] private AudioClip EnemyKillSound;
    [SerializeField] private AudioClip LegPunchSound;
    [SerializeField] private float walkFootstepInterval;
    [SerializeField] private float runFootstepInterval;
    [SerializeField] private float gearSoundDelay;


    public bool isPlayingFootsteps;

    private float footstepTimer;

    private void Start() {
        footstepTimer = walkFootstepInterval;
        isPlayingFootsteps = false;
    }

    private void Update() {
        float footstepInterval = walkFootstepInterval;
        if (isPlayingFootsteps) {
            

            if(Input.GetKey(KeyCode.LeftShift)) footstepInterval = runFootstepInterval;

            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f) {

                PlayFootstepWithGear();
                footstepTimer = footstepInterval;
            }
        } else {

            footstepTimer = footstepInterval;
        }
    }

    public void PlayFootstepWithGear() {

        if (footstepSounds.Length > 0)
        {

            AudioClip randomFootstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
            FootstepsAudioSource.PlayOneShot(randomFootstep);
        }

        Invoke(nameof(PlayRandomGearSound), gearSoundDelay);
    }

    public void PlayRandomGearSound() {

        if (gearSounds.Length > 0) {

            AudioClip randomGear = gearSounds[Random.Range(0, gearSounds.Length)];
            GearAudioSource.PlayOneShot(randomGear);
        }
    }

    public void PlayWeaponSwapSound(int WeaponSoundId = 0) {

        if (weaponSwapSounds.Length > 0 && WeaponSoundId <= weaponSwapSounds.Length) {

            AudioClip SwapSound = weaponSwapSounds[WeaponSoundId];
            GearAudioSource.PlayOneShot(SwapSound);
        }
    }

    public void PlayAmmoPickUpSound() {
        if (AmmoPickUpSound == null) return;
        
        PickUpSource.PlayOneShot(AmmoPickUpSound);
    }

    public void PlayHealthKitPickUpSound() {
        if (HealthKitPickUpSound == null) return;
        
        PickUpSource.PlayOneShot(HealthKitPickUpSound);
    }


    
}
