﻿using NSpec;
using Entitas;

class describe_ReactiveSystem : nspec {

    void when_created() {
        Pool pool = null;
        ReactiveSystem reactiveSystem = null;
        ReactiveSubSystemSpy subSystem = null;
        MultiReactiveSubSystemSpy multiSubSystem = null;
        before = () => {
            pool = new Pool(CID.NumComponents);
        };

        context["OnEntityAdded"] = () => {
            before = () => {
                subSystem = getSubSystemSypWithOnEntityAdded();
                reactiveSystem = new ReactiveSystem(pool, subSystem);
            };

            it["does not execute its subsystem when no entities were collected"] = () => {
                reactiveSystem.Execute();
                subSystem.didExecute.should_be(0);
            };

            it["executes when triggeringMatcher matches"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["executes only once when triggeringMatcher matches"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["collects changed entities in execute"] = () => {
                subSystem.replaceComponentAOnExecute = true;
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(2);
            };

            it["doesn't execute when triggeringMatcher doesn't match"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                reactiveSystem.Execute();
                subSystem.didExecute.should_be(0);
                subSystem.entities.should_be_null();
            };

            it["deactivates"] = () => {
                reactiveSystem.Deactivate();
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(0);
                subSystem.entities.should_be_null();
            };

            it["activates"] = () => {
                reactiveSystem.Deactivate();
                reactiveSystem.Activate();
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };
        };

        context["OnEntityRemoved"] = () => {
            before = () => {
                subSystem = getSubSystemSypWithOnEntityRemoved();
                reactiveSystem = new ReactiveSystem(pool, subSystem);
            };

            it["executes when triggeringMatcher matches"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                e.RemoveComponentA();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["executes only once when triggeringMatcher matches"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                e.RemoveComponentA();
                reactiveSystem.Execute();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["doesn't execute when triggeringMatcher doesn't match"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                e.AddComponentC();
                e.RemoveComponentC();
                reactiveSystem.Execute();
                subSystem.didExecute.should_be(0);
                subSystem.entities.should_be_null();
            };

            it["retains entities until execute completed"] = () => {
                var didExecute = false;
                subSystem.executeBlock = entities => {
                    var providedEntity = entities[0];
                    var newEntitty = pool.CreateEntity();
                    providedEntity.should_not_be_null();
                    providedEntity.should_not_be_same(newEntitty);
                    didExecute = true;
                };
                
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                pool.DestroyEntity(e);
                reactiveSystem.Execute();
                didExecute.should_be_true();

                var reusedEntity = pool.CreateEntity();
                reusedEntity.should_not_be_null();
                reusedEntity.should_be_same(e);
            };
        };

        context["OnEntityAddedOrRemoved"] = () => {
            it["executes when added"] = () => {
                subSystem = getSubSystemSypWithOnEntityAddedOrRemoved();
                reactiveSystem = new ReactiveSystem(pool, subSystem);
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["executes when removed"] = () => {
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                subSystem = getSubSystemSypWithOnEntityAddedOrRemoved();
                reactiveSystem = new ReactiveSystem(pool, subSystem);
                e.RemoveComponentA();
                reactiveSystem.Execute();

                subSystem.didExecute.should_be(1);
                subSystem.entities.Length.should_be(1);
                subSystem.entities.should_contain(e);
            };

            it["collects matching entities created or modified in the subsystem"] = () => {
                var sys = new EntityEmittingSubSystem(pool);
                reactiveSystem = new ReactiveSystem(pool, sys);
                var e = pool.CreateEntity();
                e.AddComponentA();
                e.AddComponentB();
                reactiveSystem.Execute();
                sys.entities.Length.should_be(1);
                reactiveSystem.Execute();
                sys.entities.Length.should_be(1);
            };
        };

        context["MultiReactiveSystem"] = () => {
            before = () => {
                var triggers = new [] {
                    Matcher.AllOf(new [] { CID.ComponentA }).OnEntityAdded(),
                    Matcher.AllOf(new [] { CID.ComponentB }).OnEntityRemoved()
                };
                multiSubSystem = new MultiReactiveSubSystemSpy(triggers);
                reactiveSystem = new ReactiveSystem(pool, multiSubSystem);
            };
            
            it["executes when a triggering matcher matches"] = () => {
                var eA = pool.CreateEntity();
                eA.AddComponentA();
                var eB = pool.CreateEntity();
                eB.AddComponentB();
                reactiveSystem.Execute();

                multiSubSystem.didExecute.should_be(1);
                multiSubSystem.entities.Length.should_be(1);
                multiSubSystem.entities.should_contain(eA);

                eB.RemoveComponentB();
                reactiveSystem.Execute();

                multiSubSystem.didExecute.should_be(2);
                multiSubSystem.entities.Length.should_be(1);
                multiSubSystem.entities.should_contain(eB);
            };
        };

        context["ensure components matcher"] = () => {

            it["only passes in entities matching required matcher"] = () => {
                var ensureSubSystem = new ReactiveEnsureSubSystemSpy(
                    allOfAB(),
                    GroupEventType.OnEntityAdded,
                    Matcher.AllOf(new [] {
                        CID.ComponentA,
                        CID.ComponentB,
                        CID.ComponentC
                    })
                );
                reactiveSystem = new ReactiveSystem(pool, ensureSubSystem);

                var eAB = pool.CreateEntity();
                eAB.AddComponentA();
                eAB.AddComponentB();
                var eABC = pool.CreateEntity();
                eABC.AddComponentA();
                eABC.AddComponentB();
                eABC.AddComponentC();
                reactiveSystem.Execute();

                ensureSubSystem.didExecute.should_be(1);
                ensureSubSystem.entities.Length.should_be(1);
                ensureSubSystem.entities.should_contain(eABC);
            };

            it["retains entities until execute completed"] = () => {
                var ensureSubSystem = new ReactiveEnsureSubSystemSpy(
                    allOfAB(),
                    GroupEventType.OnEntityAdded,
                    Matcher.AllOf(new [] {
                        CID.ComponentA,
                        CID.ComponentB,
                        CID.ComponentC
                    })
                );
                reactiveSystem = new ReactiveSystem(pool, ensureSubSystem);

                var eABC = pool.CreateEntity();

                eABC.AddComponentA();
                eABC.AddComponentB();
                eABC.AddComponentC();                
                var refCountBefore = eABC.GetRefCount();
                refCountBefore.should_be(3); // referd by pool, group and group observer

                reactiveSystem.Execute();
                var refCountAfter = eABC.GetRefCount();
               
                refCountAfter.should_be(2); // refered by pool and group
            };

            it["only passes in entities matching required matcher (multi reactive)"] = () => {
                var triggers = new [] {
                    Matcher.AllOf(new [] { CID.ComponentA }).OnEntityAdded(),
                    Matcher.AllOf(new [] { CID.ComponentB }).OnEntityRemoved()
                };
                var ensure = Matcher.AllOf(new [] {
                    CID.ComponentA,
                    CID.ComponentB,
                    CID.ComponentC
                });

                var ensureSubSystem = new MultiReactiveEnsureSubSystemSpy(triggers, ensure);
                reactiveSystem = new ReactiveSystem(pool, ensureSubSystem);

                var eAB = pool.CreateEntity();
                eAB.AddComponentA();
                eAB.AddComponentB();
                var eABC = pool.CreateEntity();
                eABC.AddComponentA();
                eABC.AddComponentB();
                eABC.AddComponentC();
                reactiveSystem.Execute();

                ensureSubSystem.didExecute.should_be(1);
                ensureSubSystem.entities.Length.should_be(1);
                ensureSubSystem.entities.should_contain(eABC);
            };

            it["doesn't call execute when no entities left after filtering"] = () => {
                var ensureSubSystem = new ReactiveEnsureSubSystemSpy(
                    allOfAB(),
                    GroupEventType.OnEntityAdded,
                    Matcher.AllOf(new [] {
                        CID.ComponentA,
                        CID.ComponentB,
                        CID.ComponentC,
                        CID.ComponentD
                    })
                );
                reactiveSystem = new ReactiveSystem(pool, ensureSubSystem);

                var eAB = pool.CreateEntity();
                eAB.AddComponentA();
                eAB.AddComponentB();
                var eABC = pool.CreateEntity();
                eABC.AddComponentA();
                eABC.AddComponentB();
                eABC.AddComponentC();
                reactiveSystem.Execute();

                ensureSubSystem.didExecute.should_be(0);
            };
        };

        context["exlude components"] = () => {
            it["only passes in entities not matching matcher"] = () => {
                var excludeSubSystem = new ReactiveExcludeSubSystemSpy(
                    allOfAB(),
                    GroupEventType.OnEntityAdded,
                    Matcher.AllOf(new [] { CID.ComponentC })
                );
                reactiveSystem = new ReactiveSystem(pool, excludeSubSystem);

                var eAB = pool.CreateEntity();
                eAB.AddComponentA();
                eAB.AddComponentB();
                var eABC = pool.CreateEntity();
                eABC.AddComponentA();
                eABC.AddComponentB();
                eABC.AddComponentC();
                reactiveSystem.Execute();

                excludeSubSystem.didExecute.should_be(1);
                excludeSubSystem.entities.Length.should_be(1);
                excludeSubSystem.entities.should_contain(eAB);
            };

            it["retains entities until execute completed"] = () => {
                var excludeSubSystem = new ReactiveExcludeSubSystemSpy(
                    allOfAB(),
                    GroupEventType.OnEntityRemoved,
                    Matcher.AllOf(new [] { CID.ComponentC })
                );
                var didExecute = false;
                excludeSubSystem.executeBlock = entities => {
                    entities.Count.should_be(1);
                    var providedEntity = entities[0];
                    var newEntitty = pool.CreateEntity();
                    providedEntity.should_not_be_null();
                    providedEntity.should_not_be_same(newEntitty);
                    didExecute = true;
                };
                reactiveSystem = new ReactiveSystem(pool, excludeSubSystem);
                

                var e1 = pool.CreateEntity();
                e1.AddComponentA();
                e1.AddComponentB();
                pool.DestroyEntity(e1);
                var e2 = pool.CreateEntity();
                e2.AddComponentA();
                e2.AddComponentB();
                e2.AddComponentC();
                e2.RemoveComponentB();
                reactiveSystem.Execute();
                didExecute.should_be_true();

                var reusedEntity = pool.CreateEntity();
                reusedEntity.should_not_be_null();
                reusedEntity.should_be_same(e1);
            };

            it["only passes in entities not matching required matcher (multi reactive)"] = () => {
                var triggers = new [] {
                    Matcher.AllOf(new [] { CID.ComponentA }).OnEntityAdded(),
                    Matcher.AllOf(new [] { CID.ComponentB }).OnEntityRemoved()
                };
                var exclude = Matcher.AllOf(new [] {
                    CID.ComponentC
                });

                var excludeSubSystem = new MultiReactiveExcludeSubSystemSpy(triggers, exclude);
                reactiveSystem = new ReactiveSystem(pool, excludeSubSystem);

                var eAB = pool.CreateEntity();
                eAB.AddComponentA();
                eAB.AddComponentB();
                var eABC = pool.CreateEntity();
                eABC.AddComponentA();
                eABC.AddComponentB();
                eABC.AddComponentC();
                reactiveSystem.Execute();

                excludeSubSystem.didExecute.should_be(1);
                excludeSubSystem.entities.Length.should_be(1);
                excludeSubSystem.entities.should_contain(eAB);
            };
        };

        context["ensure / exlude components mix"] = () => {
            it["only passes in entities"] = () => {
                var ensureExcludeSystem = new ReactiveEnsureExcludeSubSystemSpy(
                    allOfAB(),
                    GroupEventType.OnEntityAdded,
                    Matcher.AllOf(new [] { CID.ComponentA, CID.ComponentB }),
                    Matcher.AllOf(new [] { CID.ComponentC })
                );
                reactiveSystem = new ReactiveSystem(pool, ensureExcludeSystem);

                var eAB = pool.CreateEntity();
                eAB.AddComponentA();
                eAB.AddComponentB();
                var eAC = pool.CreateEntity();
                eAC.AddComponentA();
                eAC.AddComponentC();
                var eABC = pool.CreateEntity();
                eABC.AddComponentA();
                eABC.AddComponentB();
                eABC.AddComponentC();
                reactiveSystem.Execute();

                ensureExcludeSystem.didExecute.should_be(1);
                ensureExcludeSystem.entities.Length.should_be(1);
                ensureExcludeSystem.entities.should_contain(eAB);
            };

            it["retains entities until execute completed"] = () => {
                var ensureExcludeSystem = new ReactiveEnsureExcludeSubSystemSpy(
                    allOfAB(),
                    GroupEventType.OnEntityAdded,
                    Matcher.AllOf(new [] { CID.ComponentA, CID.ComponentB }),
                    Matcher.AllOf(new [] { CID.ComponentC })
                );
                reactiveSystem = new ReactiveSystem(pool, ensureExcludeSystem);

                var eAB = pool.CreateEntity();
                eAB.AddComponentA();
                eAB.AddComponentB();
                var refCountBefore = eAB.GetRefCount();
                refCountBefore.should_be(3); // referd by pool, group and group observer
                
                reactiveSystem.Execute();

                var refCountAfter = eAB.GetRefCount();
               
                refCountAfter.should_be(2); // refered by pool and group
            };
        };
    }

    ReactiveSubSystemSpy getSubSystemSypWithOnEntityAdded() {
        return new ReactiveSubSystemSpy(allOfAB(), GroupEventType.OnEntityAdded);
    }

    ReactiveSubSystemSpy getSubSystemSypWithOnEntityRemoved() {
        return new ReactiveSubSystemSpy(allOfAB(), GroupEventType.OnEntityRemoved);
    }

    ReactiveSubSystemSpy getSubSystemSypWithOnEntityAddedOrRemoved() {
        return new ReactiveSubSystemSpy(allOfAB(), GroupEventType.OnEntityAddedOrRemoved);
    }

    IMatcher allOfAB() {
        return Matcher.AllOf(new [] {
            CID.ComponentA,
            CID.ComponentB
        });
    }
}

public static class EntityExtgensions {
    public static int GetRefCount(this Entity e) {
        return (int)e.GetType().GetField("_refCount", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(e);
    }
}
