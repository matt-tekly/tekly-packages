# Game Features
The sample game is a simple idle game

## Generators
- Have a cost to upgrade
    - Effectively how many you have of the generator
- Take a period of time to generate
- After the period of time is up some resources are generated
- Can be buffed from multiple sources
    - Reducing cost
    - Increase resource output
    - Decreasing generation time
    - Automated
- Have unlocks that generate premium currency
    - Every x upgrades gives you premium currency to collect
- Must support offline time generation
- Buy multiplier

## Managers
- Gacha mechanic to collect managers
- Managers can buff things
- Managers can be collected
- Managers have cards that represent them
- Can be upgraded
    - You need to collect a certain amount of cards to upgrade them
    - Also costs premium currency
- Manager gallery

## Missions
- Manually defined
- Have several different types
- They are completed by performing different actions in the game
- Once completed you get a reward

## Ranks
- Each rank has different missions
- Each rank resets your generators but you keep your managers

## Store

# Tech Features

## Saves
- Should support versioning so migration is possible
- Would ideally be in a container format like how a zip file works

## UI Management (Glass)
- Handle loading and displaying of UI
- Organized into Layers and Panels that attach to Layers
  - Though they're called Layers they don't strictly stack
  - Layers could be used to organize the screen into different areas
- Modals

## Localization
- Barely implemented
- Need to handle pluralizing words

## Client Builder
- Need a build window

## Addressables
- Sort out content build process

## Data Models
- Add better data model window
  - Should be searchable
- Add markup to ObjectModels so we can display all model types in the model window
- ListBinder needs a way to sort models for different types of sorting the UI might need
  - Such as sorting things by date added, rarity or price
- Need a way to store a model in multiple ObjectModels
  - As is a model in an ObjectModel will be disposed the ObjectModel is disposed
  - This doesn't work well if you need a sorted collection

## Tree States
- Add Editor window for easier tracking of what is happening
  - Maybe use TreeView example for this