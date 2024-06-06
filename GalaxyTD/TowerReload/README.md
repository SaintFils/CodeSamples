ReloadingSystem picks up attacking entities in ReloadingJob and checks if reloading is currently needed. 
If it does, it reloads and creates a ReloadEvent, which is handled by OnReloadEventSystem.