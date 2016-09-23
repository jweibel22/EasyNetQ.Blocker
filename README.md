# EasyNetQ.Blocker
Framework for writing deterministic system tests with EasyNetQ

The purpose of this framework is to make it easier to write tests that validates the behaviour of a deployed distributed asynchronous system. With the recent rise in popularity of the micro services architecture, this practice is becoming increasingly more important.

As a general note, I don't recommend testing business logic with these kinds of tests. Business logic and other behaviour that is local to a specific service belongs inside that service. Besides, testing all possible combinations of state and workflows across all services is simply not feasible.  The purpose of the system tests is to validate that the infrastructure is working and that services can interact. Thus, usually only the "sunshine scenarios" are tested with a system wide test. Please read the WIKI for a more in depth discussion about the intentions behind this repo
