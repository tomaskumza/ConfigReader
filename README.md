Console application app reading simulation config files. simulation config files must be defined in app appsetings.json file in Configs folder.
Parameters:
	ConfigPath - path of simulation config file.
	ConfigPriority - configs with bigger priority can override values from configs with lower priority.

You can add as many config sections as you want. Take attention that config priorities must be unique.

Question: Please do also provide some thoughts (in textual form only) on how the following requirement could be implemented:
- it should provide information if variability constraints are violated 
	- Example: The parameter 'powerSupply' is required to be set to 'big' if the number of aisles in the sub-system config is >=5.

Answer: in this fist think who comes in my mind is FluentValidation (https://docs.fluentvalidation.net/en/latest/start.html). But then we should to know all simulation config struture and create class which represents it. Reader also then should be changed a bit. 


