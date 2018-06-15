from setuptools import setup, find_packages

exec(open('ai2thor/_version.py').read())

VERSION = __version__


setup(name='ai2thor',
      version=VERSION,
      description='AI2 Thor framework',
      long_description="AI2 Thor is a lightweight AI framework that interacts with the Unity3d Game Engine.",
      classifiers=[
          'Intended Audience :: Science/Research',
          'Development Status :: 3 - Alpha',
          'License :: OSI Approved :: Apache Software License',
          'Topic :: Scientific/Engineering :: Artificial Intelligence',
          'Programming Language :: Python',
          'Programming Language :: Python :: 2.7',
          'Programming Language :: Python :: 3',
          'Programming Language :: Python :: 3.3',
          'Programming Language :: Python :: 3.4',
          'Programming Language :: Python :: 3.5',
          'Programming Language :: Python :: 3.6',
      ],
      keywords='AI2 Thor API',
      url='https://github.com/allenai/ai2thor',
      author='Allen Institute for Artificial Intelligence',
      author_email='ai2thor@allenai.org',
      license='Apache',
      packages=find_packages(exclude=["*.tests", "*.tests.*", "tests.*", "tests"]),
      install_requires=[
          'flask',
          'numpy',
          'pyyaml',
          'requests',
          'progressbar2'
      ],
      setup_requires=['pytest-runner'],
      tests_require=['pytest', 'pytest-cov'],
      include_package_data=False)
